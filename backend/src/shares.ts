import { config } from "./index.ts";
import * as fs from 'node:fs';
import auth from "basic-auth";
import { timingSafeEqual, createHash } from "node:crypto";

function generateId(){
    return Math.random().toString(36).substr(2, 9);
}

// Credits for this function go to github/@Bruce17
function safeCompare(userInput: string, secret: string) {
	const hashUser = createHash('sha256').update(Buffer.from(userInput, 'utf8')).digest();
	const hashSecret = createHash('sha256').update(Buffer.from(secret, 'utf8')).digest();
	return timingSafeEqual(hashUser, hashSecret);
}

export type ShareCreationData = {
    winPath: string,
    user: string,
    ttl: number,
    auth: { user: string, password: string }
}

interface IShare {
    id: string;
    path: string;
    creation: {user: string, timestamp: number};
    expiry: Date;
    auth: {user: string, password: string};
}

export class Share implements IShare {

    public id: string = generateId();
    public path: string = "";
    public creation: {user: string, timestamp: number} = {user: "", timestamp: Date.now()};
    public expiry: Date = new Date(0);
    public auth: {user: string, password: string} = {user: "", password: ""};

    constructor (obj?: ShareCreationData) {
        if (obj) {
            this.path = obj.winPath;
            this.creation = {user: obj.user, timestamp: Date.now()};
            this.expiry = new Date(Date.now() + obj.ttl * 1000);
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
        return safeCompare(user, this.auth.user) && safeCompare(password, this.auth.password);
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
                if (share.hasExpired || !share.fileExists) {
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

    public exportShares() {
        fs.writeFile(this.shareDataFile, JSON.stringify(this.shares, null, 2), (err) => {
            if (err) {
                console.error("Error saving shares to disk:", err);
            }
        });
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