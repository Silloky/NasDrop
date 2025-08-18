import express, { Router, Request } from "express";
import { config, shareList } from "./index.ts";
import jsonwebtoken, { JwtPayload } from "jsonwebtoken"
import { ShareCreationData } from "./shares.ts";

declare module 'express' {
    interface Request {
        user?: string;
    }
}

const apiRouter: Router = express.Router();

function transformPath(path: string) {
    return path
}

apiRouter.use((req: Request, res, next) => {
    if (req.path === '/auth') {
        next();
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
        next();
    }
});

apiRouter.post('/auth', (req, res) => {
    const { username, password } = req.body;
    const user = config.USERS.find(u => u.username === username && u.password === password);
    if (user) {
        const token = jsonwebtoken.sign({ username: user.username }, config.JWT_SECRET, { expiresIn: '30d' });
        res.json({ token });
    } else {
        res.status(401).send("Invalid credentials");
    }
});

apiRouter.get('/shares', (req: Request, res) => {
    if (req.user) {
        const userShares = shareList.getSharesByUser(req.user);
        res.json(userShares);
    } else {
        res.status(401).send("Unauthorized");
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
    if (share) {
        share.path = (winPath ? transformPath(winPath) : undefined) ?? share.path;
        share.expiry = (ttl ? new Date(Date.now() + ttl * 1000) : undefined) ?? share.expiry;
        share.auth = auth ?? share.auth;
        res.status(200).json({ success: true });
    } else {
        res.status(404).json({ success: false, message: "Share not found" });
    }
});

apiRouter.delete('/:id', (req: Request, res) => {
    const share = shareList.getShare(req.params.id!);
    if (share) {
        shareList.removeShare(req.params.id!);
        res.status(204).send({ success: true });
    } else {
        res.status(404).json({ success: false, message: "Share not found" });
    }
})

export default apiRouter;
