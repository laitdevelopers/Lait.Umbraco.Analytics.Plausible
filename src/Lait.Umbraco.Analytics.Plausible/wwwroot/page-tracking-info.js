import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { getPageStats } from './api.js';

class LaitPageTrackingInfo extends UmbElementMixin(LitElement) {
  static properties = {
    _stats: { state: true },
    _loading: { state: true },
    _error: { state: true },
    _unique: { state: true },
  };

  constructor() {
    super();
    this._loading = true;
    this._error = null;
    this._stats = null;
    this._unique = undefined;

    this.consumeContext(UMB_AUTH_CONTEXT, (auth) => {
      this._auth = auth;
      this.#maybeLoad();
    });

    this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (workspace) => {
      if (!workspace) return;
      // The document key (unique) is an observable on the workspace context.
      this.observe(
        workspace.unique,
        (unique) => {
          this._unique = unique;
          this.#maybeLoad();
        },
        '_uniqueObserver',
      );
    });
  }

  async #token() {
    try {
      return this._auth ? await this._auth.getLatestToken() : undefined;
    } catch {
      return undefined;
    }
  }

  async #maybeLoad() {
    if (!this._auth || !this._unique) return;
    this._loading = true;
    this._error = null;
    try {
      const token = await this.#token();
      this._stats = await getPageStats(this._unique, '7d', token);
    } catch (e) {
      this._error = e?.message ?? 'Failed to load.';
    } finally {
      this._loading = false;
    }
  }

  render() {
    return html`<uui-box headline="Page tracking (last 7 days)">${this.#body()}</uui-box>`;
  }

  #body() {
    if (this._loading) return html`<uui-loader></uui-loader>`;
    if (this._error) return html`<p class="muted">${this._error}</p>`;

    const s = this._stats;
    if (!s) return html`<p class="muted">No data.</p>`;
    if (s.configured === false) return html`<p class="muted">Plausible is not configured yet.</p>`;
    if (!s.path)
      return html`<p class="muted">No tracking data — the page may be unpublished or not yet visited.</p>`;

    return html`
      <div class="headline">
        <strong>${s.pageviews}</strong> views
        <span class="muted">(${s.visitors} unique)</span>
      </div>
      <div class="path muted">${s.path}</div>
    `;
  }

  static styles = css`
    .headline {
      font-size: 1.15rem;
    }
    .headline strong {
      font-size: 1.5rem;
    }
    .muted {
      color: var(--uui-color-text-alt);
    }
    .path {
      font-size: 0.85rem;
      margin-top: var(--uui-size-space-2);
    }
  `;
}

customElements.define('lait-page-tracking-info', LaitPageTrackingInfo);
export default LaitPageTrackingInfo;
