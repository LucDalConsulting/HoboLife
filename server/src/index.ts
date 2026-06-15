// HoboLife multiplayer presence server. Tiny on purpose so it runs on a free /
// ~$5-a-month instance and stays well within the $10/mo budget.

import { Server } from 'colyseus';
import { CityRoom } from './rooms/CityRoom';

const port = Number(process.env.PORT) || 2567;

const gameServer = new Server();
gameServer.define('city', CityRoom);

gameServer
  .listen(port)
  .then(() => console.log(`HoboLife city server listening on :${port}`))
  .catch((err: unknown) => {
    console.error('Failed to start server:', err);
    process.exit(1);
  });
