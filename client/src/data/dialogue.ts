// Data-driven dialogue. A tree is a map of nodes; each option either navigates,
// leaves, starts a fight, or runs a skill check whose outcome applies effects.

import type { SkillKey } from '../core/constants';

export interface CheckOutcome {
  text: string;
  /** Cash delta (can be negative). */
  money?: number;
  /** Skill gain. */
  skill?: { key: SkillKey; amount: number };
  health?: number;
  hunger?: number;
  giveItem?: string;
  /** Close the conversation afterwards. */
  end?: boolean;
}

export type OptionEffect =
  | { type: 'goto'; node: string }
  | { type: 'leave' }
  | { type: 'fight' }
  | {
      type: 'check';
      skill: SkillKey;
      dc: number;
      success: CheckOutcome;
      fail: CheckOutcome;
    };

export interface DialogueOption {
  label: string;
  effect: OptionEffect;
}

export interface DialogueNode {
  id: string;
  text: string;
  options: DialogueOption[]; // up to 4
}

export interface DialogueTree {
  root: string;
  nodes: Record<string, DialogueNode>;
}

/** Ordinary pedestrians: panhandle, flirt, mug, or leave. */
export const PEDESTRIAN_TREE: DialogueTree = {
  root: 'start',
  nodes: {
    start: {
      id: 'start',
      text: 'The pedestrian eyes you warily. "...Yeah? What do you want?"',
      options: [
        {
          label: 'Spare some change?',
          effect: {
            type: 'check',
            skill: 'cha',
            dc: 30,
            success: {
              text: '"Here, get yourself something to eat." They hand you a few bucks.',
              money: 12,
              end: true,
            },
            fail: { text: 'They clutch their bag and hurry off.', end: true },
          },
        },
        {
          label: 'You look amazing today.',
          effect: {
            type: 'check',
            skill: 'cha',
            dc: 50,
            success: {
              text: 'They laugh, flattered. You feel a little more charming.',
              skill: { key: 'cha', amount: 1 },
              end: true,
            },
            fail: { text: '"Ugh, creep." They walk away.', end: true },
          },
        },
        {
          label: 'Hand over your wallet. (mug)',
          effect: { type: 'fight' },
        },
        { label: 'Never mind.', effect: { type: 'leave' } },
      ],
    },
  },
};

/** Hostile thugs: defuse with charisma, fight, or back off. */
export const THUG_TREE: DialogueTree = {
  root: 'start',
  nodes: {
    start: {
      id: 'start',
      text: 'The thug squares up. "You looking at me, bum?!"',
      options: [
        { label: 'Back off slowly.', effect: { type: 'leave' } },
        { label: 'Bring it.', effect: { type: 'fight' } },
        {
          label: 'Talk them down.',
          effect: {
            type: 'check',
            skill: 'cha',
            dc: 60,
            success: {
              text: '"...Tch. Get outta here." They lose interest.',
              skill: { key: 'cha', amount: 1 },
              end: true,
            },
            fail: { text: 'It only makes them angrier.', end: false },
          },
        },
      ],
    },
  },
};

export const TREES: Record<string, DialogueTree> = {
  pedestrian: PEDESTRIAN_TREE,
  thug: THUG_TREE,
};
