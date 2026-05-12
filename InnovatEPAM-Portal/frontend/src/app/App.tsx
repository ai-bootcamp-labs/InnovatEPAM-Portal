import { BrowserRouter } from 'react-router-dom';
import { Providers } from '@/app/Providers';
import { Router } from '@/app/Router';

export function App(): JSX.Element {
  return (
    <BrowserRouter>
      <Providers>
        <Router />
      </Providers>
    </BrowserRouter>
  );
}
