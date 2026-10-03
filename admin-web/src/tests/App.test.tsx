import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { App } from '../App';
import { getHealth } from '../api/health';

describe('Phase 0 workspace', () => {
  it('renders honestly without requesting the network on startup', () => {
    const fetch = vi.fn();
    vi.stubGlobal('fetch', fetch);
    render(<App />);
    expect(screen.getByRole('heading', { name: /Training happens on site/ })).toBeVisible();
    expect(screen.getAllByText('Implementation pending')).toHaveLength(2);
    expect(screen.getByRole('status')).toHaveTextContent('not been checked');
    expect(fetch).not.toHaveBeenCalled();
  });

  it('shows the version from a successful service check', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ status: 'ok', version: '0.1.0' }))));
    render(<App />);
    fireEvent.click(screen.getByRole('button', { name: 'Check connection' }));
    expect(await screen.findByText('Backend connected · version 0.1.0')).toBeVisible();
  });

  it('offers recovery when offline and allows retry', async () => {
    const fetch = vi.fn().mockRejectedValueOnce(new TypeError('offline'))
      .mockResolvedValueOnce(new Response(JSON.stringify({ status: 'ok', version: '0.1.0' })));
    vi.stubGlobal('fetch', fetch);
    render(<App />);
    fireEvent.click(screen.getByRole('button', { name: 'Check connection' }));
    expect(await screen.findByText(/Unable to reach the backend/)).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: 'Check connection' }));
    expect(await screen.findByText(/Backend connected/)).toBeVisible();
  });

  it.each([{ status: 'ok' }, { status: 'failed', version: '0.1.0' }, null])(
    'rejects malformed health response %j', async (body) => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(body))));
      await expect(getHealth()).rejects.toThrow('Unexpected health response');
    },
  );

  it('rejects HTTP failures even with a success-shaped body', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ status: 'ok', version: '0.1.0' }), { status: 503 })));
    await expect(getHealth()).rejects.toThrow('Service unavailable');
  });
});
