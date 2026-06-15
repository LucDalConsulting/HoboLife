// Tiny DOM helpers so the UI stays dependency-free.

type Props = Record<string, unknown> & { class?: string; html?: string };

export function el<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  props: Props = {},
  children: (Node | string)[] = [],
): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  const { class: cls, html, ...rest } = props;
  if (cls) node.className = cls;
  if (html !== undefined) node.innerHTML = html;
  Object.assign(node, rest);
  for (const c of children) node.append(c);
  return node;
}

export function clear(node: HTMLElement): void {
  while (node.firstChild) node.removeChild(node.firstChild);
}

export function show(node: HTMLElement, visible: boolean): void {
  node.style.display = visible ? '' : 'none';
}
