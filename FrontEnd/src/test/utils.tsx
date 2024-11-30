import { render } from '@testing-library/react';
import { ReactElement } from 'react';

export function renderWithProviders(ui: ReactElement) {
  return render(ui);
}

// Add any mock data or helper functions you'll need for tests
export const mockTmdbResponse = {
  // Add mock data structure matching your TMDB responses
};
