import express from "express";
import { Share } from "./shares.ts";
import * as path from "node:path"

export default function handleFileStreaming(share: Share, req: express.Request, res: express.Response){
    const total = share.fileSize;
    const filename = path.basename(share.path);

    res.setHeader('Accept-Ranges', 'bytes');

    const range = req.headers.range;
    if (range) {
        const match = range.match(/bytes=(\d*)-(\d*)/);
        if (!match) {
            res.setHeader('Content-Range', `bytes */${total}`);
            return res.status(416).end();
        }

        let start = match[1] ? parseInt(match[1], 10) : NaN;
        let end = match[2] ? parseInt(match[2], 10) : NaN;

        if (Number.isNaN(start) && !Number.isNaN(end)) {
            const length = end;
            start = total - length;
            end = total - 1;
        } else {
            if (Number.isNaN(start)) start = 0;
            if (Number.isNaN(end) || end >= total) end = total - 1;
        }

        if (start < 0 || end < start || start >= total) {
            res.setHeader('Content-Range', `bytes */${total}`);
            return res.status(416).end();
        }

        res.status(206);
        res.setHeader('Content-Range', `bytes ${start}-${end}/${total}`);
        res.setHeader('Content-Length', String(end - start + 1));
        res.setHeader('Content-Type', 'application/octet-stream');
        res.setHeader('Content-Disposition', `attachment; filename="${filename}"`);

        const stream = share.fileStream(start, end);
        stream.on('open', () => stream.pipe(res));
        stream.on('error', () => res.status(500).end());
        req.on('close', () => stream.destroy());
    } else {
        res.status(200);
        res.setHeader('Content-Length', String(total));
        res.setHeader('Content-Type', 'application/octet-stream');
        res.setHeader('Content-Disposition', `attachment; filename="${filename}"`);

        const stream = share.fileStream();
        stream.on('open', () => stream.pipe(res));
        stream.on('error', () => res.status(500).end());
        req.on('close', () => stream.destroy());
    }
}