import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { getStats } from './api.js';

const PERIODS = ['day', '7d', '30d', 'month'];

class LaitTrackingDashboard extends UmbElementMixin(LitElement) {
  static properties = {
    _stats: { state: true },
    _loading: { state: true },
    _error: { state: true },
    _period: { state: true },
  };

  constructor() {
    super();
    this._period = '7d';
    this._loading = true;
    this._error = null;
    this._stats = null;

    this.consumeContext(UMB_AUTH_CONTEXT, (auth) => {
      this._auth = auth;
      this.#load();
    });
  }

  async #token() {
    try {
      return this._auth ? await this._auth.getLatestToken() : undefined;
    } catch {
      return undefined;
    }
  }

  async #load() {
    if (!this._auth) return;
    this._loading = true;
    this._error = null;
    try {
      const token = await this.#token();
      this._stats = await getStats(this._period, token);
    } catch (e) {
      this._error = e?.message ?? 'Failed to load tracking data.';
    } finally {
      this._loading = false;
    }
  }

  #setPeriod(period) {
    if (period === this._period) return;
    this._period = period;
    this.#load();
  }

  render() {
    return html`
      <umb-body-layout header-transparent>
        <div id="header" slot="header">
          <h3>Site tracking</h3>
          <uui-button-group>
            ${PERIODS.map(
              (p) => html`<uui-button
                look=${this._period === p ? 'primary' : 'default'}
                label=${p}
                @click=${() => this.#setPeriod(p)}></uui-button>`,
            )}
          </uui-button-group>
        </div>
        ${this.#renderBody()}
      </umb-body-layout>
    `;
  }

  #renderBody() {
    if (this._loading) return html`<uui-loader></uui-loader>`;
    if (this._error) return html`<uui-box><p class="error">${this._error}</p></uui-box>`;
    if (this._stats?.configured === false) return this.#renderNotConfigured();
    if (this._stats?.error) return this.#renderError(this._stats.error);

    const s = this._stats ?? {};
    return html`
      <div class="grid">
        ${this.#card('Unique visitors', s.visitors)}
        ${this.#card('Page views', s.pageviews)}
        ${this.#card('Visits', s.visits)}
        ${this.#card('Bounce rate', `${s.bounceRate ?? 0}%`)}
      </div>
      <uui-box headline="Top pages">
        ${(s.topPages ?? []).length === 0
          ? html`<p>No data for this period.</p>`
          : html`
              <uui-table>
                <uui-table-row>
                  <uui-table-head-cell>Page</uui-table-head-cell>
                  <uui-table-head-cell>Visitors</uui-table-head-cell>
                </uui-table-row>
                ${s.topPages.map(
                  (p) => html`<uui-table-row>
                    <uui-table-cell>${p.page}</uui-table-cell>
                    <uui-table-cell>${p.visitors}</uui-table-cell>
                  </uui-table-row>`,
                )}
              </uui-table>
            `}
      </uui-box>
    `;
  }

  #card(label, value) {
    return html`<uui-box class="stat">
      <div class="value">${value ?? 0}</div>
      <div class="label">${label}</div>
    </uui-box>`;
  }

  #renderNotConfigured() {
    return html`<uui-box headline="Plausible is not configured yet">
      <p>
        Add your Plausible <code>SiteId</code> and <code>ApiKey</code> under the
        <code>Lait:Tracking:Plausible</code> section in <code>appsettings</code> (use user-secrets or
        environment variables for the key), then reload.
      </p>
      ${this.#renderDiagnostics()}
    </uui-box>`;
  }

  #renderError(message) {
    return html`<uui-box headline="Couldn't load tracking data">
      <p class="error">${message}</p>
      <p>
        Common causes: the <code>SiteId</code> doesn't match a site in your Plausible account, the API
        key lacks Stats access, or (self-hosted) the <code>BaseUrl</code> is wrong.
      </p>
      ${this.#renderDiagnostics()}
    </uui-box>`;
  }

  #renderDiagnostics() {
    const d = this._stats?.diagnostics ?? {};
    const ok = (cond) => html`<span class=${cond ? 'ok' : 'bad'}>${cond ? '✓' : '✗'}</span>`;
    return html`
      <p><strong>What the server can currently read:</strong></p>
      <uui-table class="diag">
        <uui-table-row>
          <uui-table-cell>BaseUrl</uui-table-cell>
          <uui-table-cell>${d.baseUrl ?? '(unknown)'}</uui-table-cell>
        </uui-table-row>
        <uui-table-row>
          <uui-table-cell>SiteId ${ok(d.siteId && d.siteId !== '(not set)')}</uui-table-cell>
          <uui-table-cell>${d.siteId ?? '(unknown)'}</uui-table-cell>
        </uui-table-row>
        <uui-table-row>
          <uui-table-cell>ApiKey ${ok(d.apiKeySet)}</uui-table-cell>
          <uui-table-cell>${d.apiKeyPreview ?? '(unknown)'}</uui-table-cell>
        </uui-table-row>
        <uui-table-row>
          <uui-table-cell>CacheSeconds</uui-table-cell>
          <uui-table-cell>${d.cacheSeconds ?? '-'}</uui-table-cell>
        </uui-table-row>
        <uui-table-row>
          <uui-table-cell>TopPagesLimit</uui-table-cell>
          <uui-table-cell>${d.topPagesLimit ?? '-'}</uui-table-cell>
        </uui-table-row>
      </uui-table>`;
  }

  static styles = css`
    :host {
      display: block;
    }
    #header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: var(--uui-size-space-4);
      margin-bottom: var(--uui-size-space-5);
    }
    .stat .value {
      font-size: 2rem;
      font-weight: 700;
      line-height: 1.1;
    }
    .stat .label {
      color: var(--uui-color-text-alt);
    }
    .error {
      color: var(--uui-color-danger);
    }
    .diag {
      max-width: 520px;
      font-family: var(--uui-font-monospace, monospace);
      font-size: 0.9rem;
    }
    .ok {
      color: var(--uui-color-positive);
      font-weight: 700;
    }
    .bad {
      color: var(--uui-color-danger);
      font-weight: 700;
    }
  `;
}

customElements.define('lait-tracking-dashboard', LaitTrackingDashboard);
export default LaitTrackingDashboard;
