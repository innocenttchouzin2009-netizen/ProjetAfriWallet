import express from 'express';

const app = express();
const port = process.env.PORT ?? 3001;

app.get('/', (_req, res) => {
  res.json({ status: 'ok' });
});

app.listen(port, () => {
  console.log(`API server listening on http://localhost:${port}`);
});
