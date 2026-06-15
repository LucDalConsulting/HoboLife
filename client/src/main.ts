// Bootstrap: load an existing save or run first-time character creation, then
// start the game. Also wires the "new version available" banner and the PWA.

import './ui/styles.css';
import { VERSION } from './core/constants';
import { GameState } from './state/GameState';
import {
  loadSave,
  newAccount,
  newCharacter,
  newSave,
  writeSave,
} from './state/persistence';
import { CharacterCreation } from './ui/CharacterCreation';
import { Game } from './game/Game';
import { el } from './ui/dom';

function startGame(state: GameState): void {
  const game = new Game(state);
  game.start();
}

function bootstrap(): void {
  const existing = loadSave();
  if (existing) {
    startGame(new GameState(existing));
    return;
  }

  const creation = new CharacterCreation();
  creation.open({ mode: 'new' }, (res) => {
    const account = newAccount(res.id);
    const character = newCharacter(res.skills, res.appearance);
    const save = newSave(account, character);
    writeSave(save);
    startGame(new GameState(save));
  });
}

// --- "update available" banner ---------------------------------------------
// After a redeploy, an already-open client notices the bumped version.json and
// offers a one-click reload. This is the cross-device update mechanism.
async function checkForUpdate(): Promise<void> {
  try {
    const res = await fetch('./version.json', { cache: 'no-store' });
    if (!res.ok) return;
    const data = (await res.json()) as { version?: string };
    if (data.version && data.version !== VERSION) showUpdateBanner(data.version);
  } catch {
    /* offline or not deployed — ignore */
  }
}

function showUpdateBanner(version: string): void {
  const reload = el('button', {}, ['Reload']);
  reload.addEventListener('click', () => location.reload());
  const banner = el('div', { id: 'update-banner' }, [
    `A new version of HoboLife (v${version}) is available. `,
    reload,
  ]);
  banner.style.display = 'block';
  document.body.append(banner);
}

// --- PWA service worker (production only) -----------------------------------
function registerSW(): void {
  if (import.meta.env.PROD && 'serviceWorker' in navigator) {
    navigator.serviceWorker.register('./sw.js').catch(() => {
      /* ignore */
    });
  }
}

bootstrap();
void checkForUpdate();
registerSW();
// Re-check for updates when the tab regains focus.
window.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') void checkForUpdate();
});
