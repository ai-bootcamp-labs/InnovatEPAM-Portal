import { forwardRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { useCategoriesQuery, useCreateIdeaMutation, useUploadAttachmentMutation } from '@/features/ideas/api';
import { ApiError } from '@/lib/api/client';

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

  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<FileError | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), mode: 'onBlur' });

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
    <form className="mx-auto max-w-2xl space-y-5" onSubmit={handleSubmit(onSubmit)} noValidate>
      <Field label="Title" placeholder="Short, descriptive title" error={errors.title?.message} {...register('title')} />

      <label className="block text-sm">
        <span className="font-medium text-foreground">Category</span>
        <select
          {...register('categoryId')}
          className="mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
          disabled={categoriesQuery.isLoading}
        >
          <option value="">Select a categoryâ€¦</option>
          {(categoriesQuery.data ?? []).map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        {errors.categoryId ? <span className="mt-1 block text-xs text-destructive">{errors.categoryId.message}</span> : null}
      </label>

      <label className="block text-sm">
        <span className="font-medium text-foreground">Description</span>
        <textarea
          rows={8}
          {...register('description')}
          className="mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
        />
        {errors.description ? (
          <span className="mt-1 block text-xs text-destructive">{errors.description.message}</span>
        ) : null}
      </label>

      <label className="block text-sm">
        <span className="font-medium text-foreground">Attachment (optional, â‰¤ 10 MB)</span>
        <input
          type="file"
          onChange={onFileChange}
          accept={ALLOWED_EXTENSIONS.join(',')}
          className="mt-1 block w-full text-sm text-muted-foreground file:mr-3 file:rounded-md file:border-0 file:bg-secondary file:px-3 file:py-2 file:text-sm file:text-secondary-foreground hover:file:bg-secondary/80"
        />
        {fileError ? <span className="mt-1 block text-xs text-destructive">{fileError.message}</span> : null}
      </label>

      {serverError ? (
        <p role="alert" className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {serverError}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={isSubmitting || createMutation.isPending || uploadMutation.isPending}
        className="rounded-md bg-primary px-5 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-60"
      >
        {isSubmitting || createMutation.isPending || uploadMutation.isPending ? 'Submittingâ€¦' : 'Submit idea'}
      </button>
    </form>
  );
}

type FieldProps = React.InputHTMLAttributes<HTMLInputElement> & { label: string; error?: string };
const Field = forwardRef<HTMLInputElement, FieldProps>(function Field({ label, error, ...rest }, ref) {
  return (
    <label className="block text-sm">
      <span className="font-medium text-foreground">{label}</span>
      <input
        ref={ref}
        {...rest}
        className="mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
      />
      {error ? <span className="mt-1 block text-xs text-destructive">{error}</span> : null}
    </label>
  );
});
