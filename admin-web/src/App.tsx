import { useEffect, useRef, useState } from 'react';
import { getHealth } from './api/health';

type ServiceState =
  | { kind: 'idle' | 'loading' | 'error' }
  | { kind: 'ready'; version: string };

export function App() {
  const [service, setService] = useState<ServiceState>({ kind: 'idle' });
  const request = useRef<AbortController | null>(null);
  useEffect(() => () => request.current?.abort(), []);

  async function checkConnection() {
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    setService({ kind: 'loading' });
    const timeout = window.setTimeout(() => controller.abort(), 5000);
    try {
      const health = await getHealth(controller.signal);
      setService({ kind: 'ready', version: health.version });
    } catch {
      setService({ kind: 'error' });
    } finally {
      window.clearTimeout(timeout);
    }
  }

  return (
    <div className="workspace">
      <aside className="sidebar" aria-label="Workspace information">
        <a className="brand" href="#main"><span className="brand-mark" aria-hidden="true">S</span>Suraksha<span>XR</span></a>
        <p className="sidebar-label">COMPLIANCE WORKSPACE</p>
        <div className="current-page">Overview <span aria-hidden="true">↗</span></div>
        <div className="sidebar-note"><span className="dot" />Built for offline training<p>Records will sync here when a device reconnects.</p></div>
        <p className="sidebar-footer">SIH26041 · Development preview</p>
      </aside>
      <main id="main">
        <header className="topbar"><span>Workspace / Overview</span><span className="badge">Phase 0 · Foundation</span></header>
        <section className="intro">
          <p className="eyebrow">SAFETY THROUGH UNDERSTANDING</p>
          <h1>Training happens on site.<br /><span>Confidence travels with it.</span></h1>
          <p className="lede">A shared view of worker training, competency certificates, and refresher needs.</p>
        </section>
        <section className="connection panel" aria-labelledby="connection-title">
          <div><p className="eyebrow">SERVICE CONNECTION</p><h2 id="connection-title">Connect your compliance workspace</h2>
            <p role="status" aria-live="polite">
              {service.kind === 'idle' && 'The backend connection has not been checked.'}
              {service.kind === 'loading' && 'Checking the backend connection…'}
              {service.kind === 'ready' && `Backend connected · version ${service.version}`}
              {service.kind === 'error' && 'Unable to reach the backend. Start the local service, then try again.'}
            </p>
          </div>
          <button onClick={checkConnection} disabled={service.kind === 'loading'}>
            {service.kind === 'loading' ? 'Checking…' : 'Check connection'}
          </button>
        </section>
        <section aria-labelledby="modules-title">
          <div className="section-heading"><h2 id="modules-title">Training modules</h2><span>Planned for the worker app</span></div>
          <div className="modules">
            <article className="panel module"><span className="module-number">01 / FIRE SAFETY</span><h3>Fire &amp; Explosion<br />Response</h3><p>Hazard recognition, configured equipment decisions, and evacuation sequencing.</p><span className="pill">Implementation pending</span></article>
            <article className="panel module"><span className="module-number">02 / GAS SAFETY</span><h3>Gas Leak &amp;<br />Confined Space Protocol</h3><p>Alarm recognition, configured PPE decisions, and buddy-system procedures.</p><span className="pill">Implementation pending</span></article>
          </div>
        </section>
        <section className="empty-state"><h2>Compliance records will appear here</h2><p>Worker records, assessment results, and certificates are not connected in this preview.</p></section>
        <footer>Demo safety content requires expert review. Training reinforces approved site SOPs and does not replace them.</footer>
      </main>
    </div>
  );
}
