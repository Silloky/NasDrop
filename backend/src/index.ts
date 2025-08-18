import { configDotenv } from "dotenv";
import ShareList from "./shares.ts";
import express from "express";
import auth from "basic-auth";
import * as fs from 'node:fs';
import handleFileStreaming from "./stream.ts";

configDotenv({path: '../.env'});
export const config = {
    SHARE_DATA_FILE: process.env.SHARE_DATA_FILE!,
    USERS: JSON.parse(fs.readFileSync(process.env.USER_DEFINITIONS_FILE!, 'utf-8')) as { username: string, password: string }[]
};

const app = express();

app.get('/:id', (req, res) => {
    const share = shareList.getShare(req.params.id);
    if (share) {
        if (share.auth.user && share.auth.password) {
            const credentials = auth(req);
            if (!credentials || !share.checkAuth(credentials.name, credentials.pass)) {
                res.set('WWW-Authenticate', `Basic realm="${share.id}"`);
                return res.status(401).send('Authentication required.');
            }
        }

        handleFileStreaming(share, req, res)
        
    } else {
        res.status(404).send("");
    }
});


app.listen(3000, () => {
    console.log("Server is running on port 3000");
});

// Testing the ShareList class
const shareList = new ShareList(config.SHARE_DATA_FILE, true);

// var id = shareList.addShare({
//     winPath: "/path/to/share",
//     user: "user1",
//     ttl: 30,
//     auth: { user: "user1", password: "password1" }
// })
// console.log(id)
// console.log(shareList.getShare(id)?.checkAuth("user1", "password1"))


process.on('SIGINT', () => {
    shareList.exportShares();
    process.exit();
});