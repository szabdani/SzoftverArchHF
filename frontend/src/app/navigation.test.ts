import { describe, expect, it } from 'vitest';
import { navItemsFor } from './navigation';

/** A menü a szerepkörök szerint. */
describe('navItemsFor', () => {
  it('a futó a saját nézeteit látja', () => {
    expect(navItemsFor(['runner']).map((item) => item.to)).toEqual(['/', '/workouts', '/alerts']);
  });

  it('az edző és az admin a sajátjait; szerepkör nélkül üres', () => {
    expect(navItemsFor(['coach']).map((item) => item.to)).toEqual(['/coach']);
    expect(navItemsFor(['admin']).map((item) => item.to)).toEqual(['/admin/users', '/admin/rules']);
    expect(navItemsFor([])).toEqual([]);
  });
});
