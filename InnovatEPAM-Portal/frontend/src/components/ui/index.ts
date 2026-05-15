/**
 * UI primitives barrel (008-ui-polish T019). Feature pages should
 * import from `@/components/ui` only, never reach into individual files.
 */
export { Button } from './Button';
export type { ButtonProps, ButtonVariant, ButtonSize } from './Button';

export { Input } from './Input';
export type { InputProps } from './Input';

export { Textarea } from './Textarea';
export type { TextareaProps } from './Textarea';

export { Select } from './Select';
export type { SelectProps } from './Select';

export { Label } from './Label';
export type { LabelProps } from './Label';

export { FieldError } from './FieldError';
export type { FieldErrorProps } from './FieldError';

export { StatusBadge } from './StatusBadge';
export type { StatusBadgeProps, IdeaStatus } from './StatusBadge';

export { Card, CardHeader, CardBody, CardFooter } from './Card';
export type { CardProps } from './Card';

export { EmptyState } from './EmptyState';
export type { EmptyStateProps } from './EmptyState';

export { LoadingSkeleton, Skeleton, Spinner } from './LoadingSkeleton';

export { ScoreInput } from './ScoreInput';
export type { ScoreInputProps } from './ScoreInput';
