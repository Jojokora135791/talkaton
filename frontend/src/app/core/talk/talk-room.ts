/**
 * Ссылка на комнату Толка. Хост вынесен сюда одной константой: на этапе 6
 * календарь окажется внутри самого Толка, и переход станет внутренним роутом.
 */
const TALK_HOST = 'https://talk.kontur.ru/c';

export function talkRoomUrl(slug: string | null): string | null {
  return slug ? `${TALK_HOST}/${slug}` : null;
}

/** То, что показывается на карточке: без схемы, как на макете. */
export function talkRoomLabel(slug: string | null): string | null {
  return slug ? `talk.kontur.ru/c/${slug}` : null;
}

export function joinTalkRoom(slug: string | null): void {
  const url = talkRoomUrl(slug);
  if (url) {
    window.open(url, '_blank', 'noopener');
  }
}
