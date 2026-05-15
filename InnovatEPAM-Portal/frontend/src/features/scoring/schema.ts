import { z } from 'zod';

/** Phase 7 — client-side mirror of SubmitScoreRequestValidator (FR-006/FR-008). */
const dimension = z
  .number({ invalid_type_error: 'Required' })
  .int()
  .min(1, 'Rate 1-5')
  .max(5, 'Rate 1-5');

export const submitScoreSchema = z.object({
  impact: dimension,
  feasibility: dimension,
  innovation: dimension,
  alignment: dimension,
  comment: z
    .string()
    .max(1000, 'Max 1000 characters')
    .nullable()
    .transform((v) => {
      if (v == null) return null;
      const trimmed = v.trim();
      return trimmed.length === 0 ? null : trimmed;
    }),
});

export type SubmitScoreInput = z.infer<typeof submitScoreSchema>;
