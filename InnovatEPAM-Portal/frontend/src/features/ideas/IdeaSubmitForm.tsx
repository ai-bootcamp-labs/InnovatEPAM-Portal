import { useId, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useCategoriesQuery, useCreateIdeaMutation, useUploadAttachmentMutation } from '@/features/ideas/api';
import { ApiError } from '@/lib/api/client';
import { Button, FieldError, Input, Label, Select, Textarea } from '@/components/ui';

const MAX_FILE_BYTES = 10 * 1024 * 1024;
const ALLOWED_EXTENSIONS = ['.pdf', '.png', '.jpg', '.jpeg', '.docx', '.pptx', '.xlsx'] as const;
const ALLOWED_MIME = new Set([
  'application/pdf',
  'image/png',
  'image/jpeg',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
]);

const schema = z.object({
  title: z.string().min(5, 'Title must be at least 5 characters.').max(120, 'Title must be 120 characters or fewer.'),
  description: z
    .string()
    .min(1, 'Description is required.')
    .max(4000, 'Description must be 4000 characters or fewer.'),
  categoryId: z.string().uuid('Pick a category.'),
});

type FormValues = z.infer<typeof schema>;

interface FileError { message: string }

/** Idea submission form (T074) — handles optional attachment in a single flow. */
export function IdeaSubmitForm(): JSX.Element {
  const categoriesQuery = useCategoriesQuery();
  const createMutation = useCreateIdeaMutation();
  const uploadMutation = useUploadAttachmentMutation();
  const navigate = useNavigate();
  const formId = useId();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<FileError | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), mode: 'onBlur' });

  const titleId = `${formId}-title`;
  const titleErrId = `${titleId}-error`;
  const categoryId = `${formId}-category`;
  const categoryErrId = `${categoryId}-error`;
  const descriptionId = `${formId}-description`;
  const descriptionErrId = `${descriptionId}-error`;
  const attachmentId = `${formId}-attachment`;
  const attachmentErrId = `${attachmentId}-error`;

  const isPending = isSubmitting || createMutation.isPending || uploadMutation.isPending;

  function onFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    setFileError(null);
    const next = e.target.files?.[0] ?? null;
    if (!next) {
      setFile(null);
      return;
    }
    const ext = next.name.toLowerCase().slice(next.name.lastIndexOf('.'));
    if (!ALLOWED_EXTENSIONS.includes(ext as (typeof ALLOWED_EXTENSIONS)[number])) {
      setFile(null);
      setFileError({ message: 'Unsupported file type. Allowed: PDF, PNG, JPG, DOCX, PPTX, XLSX.' });
      return;
    }
    if (next.type && !ALLOWED_MIME.has(next.type)) {
      setFile(null);
      setFileError({ message: 'File MIME type does not match the allow-list.' });
      return;
    }
    if (next.size > MAX_FILE_BYTES) {
      setFile(null);
      setFileError({ message: 'File exceeds the 10 MB limit.' });
      return;
    }
    setFile(next);
  }

  async function onSubmit(values: FormValues) {
    setServerError(null);
    try {
      const idea = await createMutation.mutateAsync(values);
      if (file) {
        await uploadMutation.mutateAsync({ ideaId: idea.id, file });
      }
      navigate(`/ideas/${idea.id}`, { replace: true });
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.problem.detail ?? err.problem.title ?? 'Failed to submit idea.'
          : 'Failed to submit idea.';
      setServerError(message);
    }
  }

  return (
    <form className="mx-auto max-w-2xl space-y-6" onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="space-y-1">
        <Label htmlFor={titleId} required>
          Title
        </Label>
        <Input
          id={titleId}
          placeholder="Short, descriptive title"
          aria-required
          aria-describedby={errors.title ? titleErrId : undefined}
          invalid={Boolean(errors.title)}
          disabled={isPending}
          {...register('title')}
        />
        <FieldError id={titleErrId}>{errors.title?.message}</FieldError>
      </div>

      <div className="space-y-1">
        <Label htmlFor={categoryId} required>
          Category
        </Label>
        <Select
          id={categoryId}
          aria-required
          aria-describedby={errors.categoryId ? categoryErrId : undefined}
          invalid={Boolean(errors.categoryId)}
          disabled={categoriesQuery.isLoading || isPending}
          {...register('categoryId')}
        >
          <option value="">Select a category…</option>
          {(categoriesQuery.data ?? []).map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </Select>
        <FieldError id={categoryErrId}>{errors.categoryId?.message}</FieldError>
      </div>

      <div className="space-y-1">
        <Label htmlFor={descriptionId} required>
          Description
        </Label>
        <Textarea
          id={descriptionId}
          rows={8}
          aria-required
          aria-describedby={errors.description ? descriptionErrId : undefined}
          invalid={Boolean(errors.description)}
          disabled={isPending}
          {...register('description')}
        />
        <FieldError id={descriptionErrId}>{errors.description?.message}</FieldError>
      </div>

      <div className="space-y-1">
        <Label htmlFor={attachmentId}>Attachment (optional, ≤ 10 MB)</Label>
        <div className="flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={isPending}
            onClick={() => fileInputRef.current?.click()}
          >
            {file ? 'Replace file' : 'Choose file'}
          </Button>
          {file ? (
            <span
              className="inline-flex max-w-full items-center gap-2 rounded-full border border-border bg-secondary px-3 py-1 text-xs text-secondary-foreground"
              data-testid="attachment-chip"
            >
              <span className="truncate">{file.name}</span>
              {/* ui-polish-exception: tiny icon-only remove control inside the
                  attachment chip; using the Button primitive would force the
                  shared size/padding rhythm that doesn't fit a 16-px glyph. */}
              <button
                type="button"
                onClick={() => {
                  setFile(null);
                  setFileError(null);
                  if (fileInputRef.current) fileInputRef.current.value = '';
                }}
                aria-label={`Remove ${file.name}`}
                disabled={isPending}
                className="rounded-full text-muted-foreground hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1 disabled:opacity-50"
              >
                <span aria-hidden="true">×</span>
              </button>
            </span>
          ) : (
            <span className="text-xs text-muted-foreground">No file chosen.</span>
          )}
          {/* ui-polish-exception: visually-hidden native file input; the Input
              primitive renders a generic text-style control that's not appropriate
              for `type="file"`. The visible affordance is the Button above. */}
          <input
            ref={fileInputRef}
            id={attachmentId}
            type="file"
            onChange={onFileChange}
            accept={ALLOWED_EXTENSIONS.join(',')}
            aria-describedby={fileError ? attachmentErrId : undefined}
            aria-invalid={fileError ? true : undefined}
            disabled={isPending}
            className="sr-only"
          />
        </div>
        <FieldError id={attachmentErrId}>{fileError?.message}</FieldError>
      </div>

      {serverError ? (
        <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {serverError}
        </p>
      ) : null}

      <Button
        type="submit"
        variant="primary"
        loading={isPending}
        disabled={isPending}
      >
        {isPending ? 'Submitting…' : 'Submit idea'}
      </Button>
    </form>
  );
}
