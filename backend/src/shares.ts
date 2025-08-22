import { config } from "./index";
import * as fs from 'node:fs';
import { timingSafeEqual, createHash } from "node:crypto";

function generateId(){
    return Math.random().toString(36).substr(2, 9);
}

function safeCompare(userInput: string, secret: string) {
	const hashUser = createHash('sha256').update(Buffer.from(userInput, 'utf8')).digest();
	const hashSecret = createHash('sha256').update(Buffer.from(secret, 'utf8')).digest();
	return timingSafeEqual(hashUser, hashSecret);
}

export function servercizePath(path: string, user: string) {
    path = path.replace(/\\/g, '/');
    for (const mapping of config.MAPPINGS) {
        const regex = new RegExp(mapping.windows.replace(/\$(\d+)/g, '(.*)'));
        const match = path.match(regex);
        if (match) {
            let mapped = mapping.map.replace('%user%', user);
            mapped = mapped.replace(/\$(\d+)/g, (_, group) => match[parseInt(group)] ?? '');
            return mapped;
        }
    }
    return path;
}

export function deservercizePath(path: string, user: string) {
    for (const mapping of config.MAPPINGS) {
        const regex = new RegExp(mapping.map.replace('%user%', user).replace(/\$(\d+)/g, '(.*)'));
        const match = path.match(regex);
        if (match) {
            let mapped = mapping.windows.replace(/\$(\d+)/g, (_, group) => match[parseInt(group)] ?? '');
            return mapped.replace(/\//g, '\\');
        }
    }
    path = path.replace(/\\/g, '/');
    return path;
}

export type ShareCreationData = {
    winPath: string,
    user: string,
    ttl: number,
    auth: { username: string, password: string }
}

interface IShare {
    id: string;
    path: string;
    creation: {user: string, timestamp: Date};
    expiry: Date;
    auth: {username: string, password: string};
}

export class Share implements IShare {

    public id: string = generateId();
    public path: string = "";
    public creation: {user: string, timestamp: Date} = {user: "", timestamp: new Date()};
    public expiry: Date = new Date(0);
    public auth: {username: string, password: string} = {username: "", password: ""};
    public accessCount: number = 0;

    constructor (obj?: ShareCreationData) {
        if (obj) {
            this.path = servercizePath(obj.winPath, obj.user);
            this.creation = {user: obj.user, timestamp: new Date()};
            this.expiry = obj.ttl != -1 ? new Date(Date.now() + obj.ttl * 1000) : new Date(new Date().getFullYear() + 20, 1, 1);
            this.auth = obj.auth;
        }
    }

    public importData(data: IShare) {
        Object.assign(this, data);
        return this
    }

    public get hasExpired(): boolean {
        return this.expiry < new Date();
    }
    public get fileExists(): boolean {
        return fs.existsSync(this.path)
    }

    public checkAuth(user: string, password: string): boolean {
        return safeCompare(user, this.auth.username) && safeCompare(password, this.auth.password);
    }

    public fileStream(start?: number, end?: number): fs.ReadStream {
        return fs.createReadStream(this.path, {start, end});
    }
    public get fileSize(): number {
        return fs.statSync(this.path).size;
    }
}

class ShareList {

    private shares: Share[] = []
    private shareDataFile: string = config.SHARE_DATA_FILE;

    constructor(shareDataFile: string, importOldShares: boolean) {
        this.shareDataFile = shareDataFile;
        if (importOldShares) {
            const oldShares = JSON.parse(fs.readFileSync(shareDataFile, 'utf-8')) as IShare[];
            oldShares.forEach(share => {
                this.shares.push(new Share().importData(share));
            });
        }

        // Automatically remove expired shares and save shares to JSON file
        setInterval(() => {

            // Garbage collection
            this.shares.forEach(share => {
                //if (share.hasExpired || !share.fileExists) {
                if (share.hasExpired) {
                    this.removeShare(share.id);
                }
            });
            
            // Saving to disk
            this.exportShares()
        }, 5000);
    }

    public addShare(share: ShareCreationData): string {
        const shareObj = new Share(share)
        this.shares.push(shareObj)
        return shareObj.id
    }

    public async exportShares() {
        await fs.promises.writeFile(this.shareDataFile, JSON.stringify(this.shares, null, 2));
    }

    public getShare(id: string): Share | undefined {
        return this.shares.find(share => share.id === id);
    }

    public getSharesByUser(user: string): Share[] {
        return this.shares.filter(share => share.creation.user === user && !share.hasExpired);
    }

    public removeShare(id: string): boolean {
        const index = this.shares.findIndex(share => share.id === id);
        if (index !== -1) {
            this.shares.splice(index, 1);
            return true;
        }
        return false;
    }
}

export default ShareList;