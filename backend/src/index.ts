import { configDotenv } from "dotenv";
import ShareList from "./shares.ts";
import express from "express";
import auth from "basic-auth";
import * as fs from 'node:fs';
import handleFileStreaming from "./stream.ts";
import apiRouter from "./api.ts";

configDotenv({path: '../.env'});
export const config = {
    SHARE_DATA_FILE: process.env.SHARE_DATA_FILE!,
    USERS: JSON.parse(fs.readFileSync(process.env.USER_DEFINITIONS_FILE!, 'utf-8')) as { username: string, password: string }[],
    PUBLIC_PORT: process.env.PUBLIC_PORT ? parseInt(process.env.PUBLIC_PORT) : 3000,
    PRIVATE_PORT: process.env.PRIVATE_PORT ? parseInt(process.env.PRIVATE_PORT) : 3001,
    JWT_SECRET: process.env.JWT_SECRET!,
    PUBLIC_ENDPOINT: process.env.PUBLIC_ENDPOINT!,
    PRIVATE_ENDPOINT: process.env.PRIVATE_ENDPOINT!,
    MAPPINGS: JSON.parse(fs.readFileSync(process.env.MAPPINGS_FILE!, 'utf-8')) as { windows: string, map: string }[],
};

const public_app = express();
public_app.get('/:id', (req, res) => {
    const share = shareList.getShare(req.params.id!);
    if (share) {
        if (share.auth.username && share.auth.password) {
            const credentials = auth(req);
            if (!credentials || !share.checkAuth(credentials.name, credentials.pass)) {
                res.set('WWW-Authenticate', `Basic realm="${share.id}"`);
                res.status(401).send('Authentication required.');
                return
            }
        }

        handleFileStreaming(share, req, res)
        
    } else {
        res.status(404).send("");
    }
});
public_app.listen(config.PUBLIC_PORT, () => {
    console.log(`Public server is running on port ${config.PUBLIC_PORT}`);
});

const private_app = express();
private_app.use(express.json());
private_app.use('/api', apiRouter);
private_app.listen(config.PRIVATE_PORT, () => {
    console.log(`Private server is running on port ${config.PRIVATE_PORT}`);
});

export const shareList = new ShareList(config.SHARE_DATA_FILE, true);

process.on('SIGINT', async () => {
    await shareList.exportShares();
    process.exit();
});