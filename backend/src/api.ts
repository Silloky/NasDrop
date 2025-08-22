import express, { Router, Request } from "express";
import { config, shareList } from "./index.ts";
import jsonwebtoken, { JwtPayload } from "jsonwebtoken"
import { ShareCreationData } from "./shares.ts";
import { servercizePath, deservercizePath } from "./shares.ts";

declare module 'express' {
    interface Request {
        user?: string;
    }
}

const apiRouter: Router = express.Router();

apiRouter.use((req: Request, res, next) => {
    if (req.path === '/auth' || req.path === '/ping') {
        next();
        return
    }

    const token = req.headers.authorization?.split(" ")[1];
    if (token) {
        jsonwebtoken.verify(token, config.JWT_SECRET, (err, decoded) => {
            if (err) {
                return res.status(401).send("Invalid token");
            }
            req.user = (decoded as JwtPayload).username!;
            next();
        });
    } else {
        res.status(401).send("Unauthorized");
    }
});

apiRouter.get('/ping', (req, res) => {
    res.status(200).send(config.PUBLIC_ENDPOINT);
})

apiRouter.post('/auth', (req, res) => {
    const { username, password } = req.body;
    const user = config.USERS.find(u => u.username === username && u.password === password);
    if (user) {
        const token = jsonwebtoken.sign({ username: user.username }, config.JWT_SECRET, { expiresIn: '365d' });
        res.json({ token, expiry: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000) });
    } else {
        res.status(401).send({ error: "Invalid credentials" });
    }
});

apiRouter.get('/shares', (req: Request, res) => {
    if (req.user) {
        const userShares = shareList.getSharesByUser(req.user);
        const responseShares = userShares.map(share => ({
            ...share,
            path: deservercizePath(share.path, share.creation.user)
        }))
        res.json(responseShares);
    } else {
        res.status(401).send({ error: "Unauthorized" });
    }
});

apiRouter.put('/', (req: Request, res) => {
    const { winPath, ttl, auth }: Omit<ShareCreationData, 'user'> = req.body;
    const newShare = {
        winPath,
        user: req.user!,
        ttl,
        auth
    };
    const shareId = shareList.addShare(newShare);
    res.status(201).json({ id: shareId });
});

apiRouter.patch('/:id', (req: Request, res) => {
    const {winPath, ttl, auth}: Partial<Omit<ShareCreationData, 'user'>> = req.body;
    const share = shareList.getShare(req.params.id!);
    if (share && share.creation.user === req.user) {
        share.path = (winPath ? servercizePath(winPath, req.user) : undefined) ?? share.path;
        share.expiry = (ttl ? new Date(Date.now() + ttl * 1000) : undefined) ?? share.expiry;
        share.auth = auth ?? share.auth;
        res.status(200).json({ success: true });
    } else {
        res.status(404).json({ success: false, message: "Share not found" });
    }
});

apiRouter.delete('/:id', (req: Request, res) => {
    const share = shareList.getShare(req.params.id!);
    if (share && share.creation.user === req.user) {
        shareList.removeShare(req.params.id!);
        res.status(204).send({ success: true });
    } else {
        res.status(404).json({ success: false, message: "Share not found" });
    }
})

export default apiRouter;
