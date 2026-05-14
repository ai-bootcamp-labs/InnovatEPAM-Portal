import { IdeaSubmitForm } from '@/features/ideas/IdeaSubmitForm';
import { Container } from '@/components/layout/Container';

/** Wrapper page (T074). Embeds the form inside the standard page chrome. */
export function IdeaCreatePage(): JSX.Element {
  return (
    <Container as="main" className="max-w-4xl py-8">
      <h1 className="text-3xl font-semibold tracking-tight">Submit a new idea</h1>
      <p className="mt-1 text-sm text-muted-foreground">Share your idea with the InnovatEPAM team.</p>
      <div className="mt-8">
        <IdeaSubmitForm />
      </div>
    </Container>
  );
}
