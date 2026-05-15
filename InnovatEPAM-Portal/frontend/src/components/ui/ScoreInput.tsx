import { useCallback, useId, useRef } from 'react';
import { cn } from '@/lib/ui/cn';

/**
 * Phase 7 — 5-point rating input (FR-008, FR-011).
 *
 * Renders as an ARIA radiogroup so screen readers announce the dimension
 * label + current value. Keyboard model: <kbd>Left/Right</kbd> and
 * <kbd>Up/Down</kbd> step through the buttons; <kbd>Home</kbd>/<kbd>End</kbd>
 * jump to 1 / 5. Only one button is in the tab sequence at a time
 * (roving-tabindex) per WAI-ARIA Authoring Practices.
 */
export interface ScoreInputProps {
  label: string;
  value: number | null;
  onChange: (next: number) => void;
  /** Field id used to link external `<label>`-style elements (form errors etc.). */
  name: string;
  disabled?: boolean;
  describedBy?: string;
}

const RATINGS = [1, 2, 3, 4, 5] as const;

export function ScoreInput({
  label,
  value,
  onChange,
  name,
  disabled = false,
  describedBy,
}: ScoreInputProps): JSX.Element {
  const groupId = useId();
  const itemRefs = useRef<Array<HTMLButtonElement | null>>([]);

  const focus = useCallback((index: number) => {
    const clamped = Math.max(0, Math.min(RATINGS.length - 1, index));
    itemRefs.current[clamped]?.focus();
  }, []);

  const onKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (disabled) return;
    const currentIndex = value ? RATINGS.indexOf(value as (typeof RATINGS)[number]) : -1;
    const focusAt = (i: number) => focus(i);
    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown': {
        event.preventDefault();
        const next = currentIndex < 0 ? 0 : Math.min(RATINGS.length - 1, currentIndex + 1);
        onChange(RATINGS[next]!);
        focusAt(next);
        break;
      }
      case 'ArrowLeft':
      case 'ArrowUp': {
        event.preventDefault();
        const next = currentIndex <= 0 ? 0 : currentIndex - 1;
        onChange(RATINGS[next]!);
        focusAt(next);
        break;
      }
      case 'Home':
        event.preventDefault();
        onChange(RATINGS[0]!);
        focusAt(0);
        break;
      case 'End':
        event.preventDefault();
        onChange(RATINGS[RATINGS.length - 1]!);
        focusAt(RATINGS.length - 1);
        break;
      default:
        break;
    }
  };

  return (
    <div className="flex flex-col gap-1">
      <span id={`${groupId}-label`} className="text-sm font-medium text-foreground">
        {label}
      </span>
      <div
        role="radiogroup"
        tabIndex={-1}
        aria-labelledby={`${groupId}-label`}
        aria-describedby={describedBy}
        aria-disabled={disabled || undefined}
        data-testid={`score-input-${name}`}
        onKeyDown={onKeyDown}
        className="inline-flex gap-1"
      >
        {RATINGS.map((rating, index) => {
          const selected = value === rating;
          // Roving tabindex: first item OR the current value is reachable via Tab.
          const tabIndex =
            value == null ? (index === 0 ? 0 : -1) : selected ? 0 : -1;
          return (
            <button
              key={rating}
              ref={(el) => (itemRefs.current[index] = el)}
              type="button"
              role="radio"
              aria-checked={selected}
              aria-label={`${rating} out of 5`}
              tabIndex={tabIndex}
              disabled={disabled}
              onClick={() => onChange(rating)}
              data-rating={rating}
              data-selected={selected || undefined}
              className={cn(
                'inline-flex h-9 w-9 items-center justify-center rounded-md border text-sm font-semibold',
                'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
                'disabled:cursor-not-allowed disabled:opacity-60',
                selected
                  ? 'border-primary bg-primary text-primary-foreground'
                  : 'border-border bg-background text-foreground hover:bg-accent hover:text-accent-foreground',
              )}
            >
              {rating}
            </button>
          );
        })}
      </div>
    </div>
  );
}
