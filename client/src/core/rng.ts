// Random helpers. Centralised so dice behaviour is consistent and testable.

/** Roll a single ten-sided die (1..10 inclusive). */
export function rollD10(): number {
  return 1 + Math.floor(Math.random() * 10);
}

/** Inclusive integer in [min, max]. */
export function randInt(min: number, max: number): number {
  return min + Math.floor(Math.random() * (max - min + 1));
}

/** Float in [min, max). */
export function randFloat(min: number, max: number): number {
  return min + Math.random() * (max - min);
}

/** Pick a random element. */
export function pick<T>(arr: readonly T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

/** Clamp a number to [min, max]. */
export function clamp(v: number, min: number, max: number): number {
  return v < min ? min : v > max ? max : v;
}
