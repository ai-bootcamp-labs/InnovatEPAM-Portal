import { describe, expect, it } from 'vitest';
import { cn } from '../cn';

describe('cn (T008)', () => {
  it('lets later tailwind utilities win conflicts', () => {
    expect(cn('px-2', 'px-4')).toBe('px-4');
    expect(cn('text-sm font-medium', 'text-base')).toBe('font-medium text-base');
  });

  it('drops falsy inputs', () => {
    expect(cn('a', false && 'b', null, undefined, '', 'c')).toBe('a c');
  });

  it('preserves Tailwind variants', () => {
    const result = cn('bg-background hover:bg-accent', 'focus-visible:ring-2');
    expect(result).toContain('hover:bg-accent');
    expect(result).toContain('focus-visible:ring-2');
  });

  it('supports conditional objects and arrays from clsx', () => {
    expect(cn('base', { active: true, disabled: false }, ['extra', 'more'])).toBe(
      'base active extra more',
    );
  });
});
