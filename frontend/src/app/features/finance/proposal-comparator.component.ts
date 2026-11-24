import { CurrencyPipe, DatePipe, DecimalPipe, SlicePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface Proposal {
  institution: string;
  operation: string;
  amount: number | null;
  downPayment: number | null;
  monthlyRate: number | null;
  annualRate: number | null;
  cet: number | null;
  installments: number | null;
  installmentValue: number | null;
  iof: number | null;
  fees: number | null;
  insurance: number | null;
  otherCharges: number | null;
  chargesIncluded: boolean;
}
interface MarketCatalog { modalities: string[]; referenceMonth: string; retrievedAt: string; sourceUrl: string; rates: { institution: string; modality: string; referenceMonth: string; monthlyRatePercent: number; annualRatePercent: number }[] }

const newProposal = (): Proposal => ({
  institution: '', operation: '', amount: null, downPayment: null,
  monthlyRate: null, annualRate: null, cet: null, installments: null,
  installmentValue: null, iof: 0, fees: 0, insurance: 0, otherCharges: 0,
  chargesIncluded: true
});

@Component({
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe, DecimalPipe, SlicePipe],
  template: `
    <section class="page proposal-page" aria-labelledby="proposal-title">
      <span class="eyebrow">FERRAMENTA EDUCATIVA</span>
      <h1 id="proposal-title">Compare propostas bancárias</h1>
      <p class="lead">Informe os dados recebidos e compare custos, CET, prazo e parcelas lado a lado. Os valores ficam somente nesta página do navegador.</p>

      <div class="privacy-note" role="note"><strong>Privacidade:</strong> não informe CPF, conta, senha ou outros dados pessoais. Nenhuma proposta é enviada ou salva.</div>

      <section class="market-reference" aria-labelledby="market-title">
        <div><span class="eyebrow">REFERÊNCIA PÚBLICA</span><h2 id="market-title">Taxa média do Banco Central</h2>
          @if (marketLoading()) { <p role="status">Consultando a série oficial…</p> }
          @else if (marketRate()) { <p class="market-value">{{ marketRate()!.annualRatePercent | number:'1.2-2':'pt-BR' }}% ao ano</p><p>Crédito pessoal livre para pessoas físicas · referência {{ marketRate()!.referenceMonth | slice:0:7 }} · obtida em {{ marketRate()!.retrievedAt | date:'dd/MM/yyyy HH:mm' }}</p> }
          @else { <p>Não foi possível consultar agora. A comparação manual continua disponível.</p> }
          <small>Média de mercado das novas operações; não é oferta individual nem garantia de aprovação.</small>
        </div>
        <a href="https://dadosabertos.bcb.gov.br/dataset/20748-taxa-media-de-juros-das-operacoes-de-credito-com-recursos-livres---pessoas-fisicas---credito-" target="_blank" rel="noopener noreferrer">Fonte e metodologia no Banco Central ↗</a>
      </section>

      <section class="market-catalog" aria-labelledby="catalog-title">
        <span class="eyebrow">PESQUISA PÚBLICA</span><h2 id="catalog-title">Médias por instituição e modalidade</h2>
        <p>Dados médios mensais do Banco Central; não são cotações nem ofertas individuais.</p>
        @if (catalogLoading()) { <p role="status">Carregando taxas de referência…</p> }
        @else if (catalog(); as data) {
          <div class="catalog-controls"><label>Modalidade<select [ngModel]="selectedModality()" (ngModelChange)="loadModality($event)"><option value="">Selecione uma modalidade</option>@for (item of data.modalities; track item) { <option [value]="item">{{ item }}</option> }</select></label><label>Filtrar instituição<input [ngModel]="bankSearch()" (ngModelChange)="bankSearch.set($event)" placeholder="Nome do banco" /></label></div>
          <p>Mês de referência: {{ data.referenceMonth }} · <a [href]="data.sourceUrl" target="_blank" rel="noopener noreferrer">Fonte: Banco Central</a></p>
          @if (visibleRates().length) { <div class="table-wrap"><table><caption>Taxas médias observadas, não ofertas garantidas</caption><thead><tr><th>Instituição</th><th>Ao mês</th><th>Ao ano</th><th></th></tr></thead><tbody>@for (row of visibleRates(); track row.institution) { <tr><td>{{ row.institution }}</td><td>{{ row.monthlyRatePercent | number:'1.2-3':'pt-BR' }}%</td><td>{{ row.annualRatePercent | number:'1.2-3':'pt-BR' }}%</td><td><button class="secondary" type="button" (click)="useRate(row)">Usar como referência</button></td></tr> }</tbody></table></div> }
          @else { <p>Selecione uma modalidade para consultar as instituições disponíveis.</p> }
        } @else { <p>Consulta indisponível. Você ainda pode comparar propostas manualmente.</p> }
      </section>

      <div class="proposal-actions"><span>{{ offers().length }} propostas</span><div><button type="button" class="secondary" (click)="duplicateOffer()">Duplicar última</button> <button type="button" class="secondary" (click)="normalizeAmounts()">Criar cópias com mesmo principal</button> <button type="button" class="secondary" (click)="clearOffers()">Limpar</button> <button type="button" class="secondary" (click)="addOffer()">Adicionar proposta</button></div></div>

      <div class="proposal-grid">
        @for (offer of offers(); track $index; let i = $index) {
          <fieldset class="offer-card">
            <legend>Proposta {{ i + 1 }}</legend>
            @if (offers().length > 2) { <button class="remove" type="button" (click)="removeOffer(i)" [attr.aria-label]="'Remover proposta ' + (i + 1)">Remover</button> }
            <label>Instituição financeira<input [name]="'institution-' + i" autocomplete="off" [ngModel]="offer.institution" (ngModelChange)="update(i,'institution',$event)" placeholder="Nome do banco" /></label>
            <label>Tipo de operação<input [name]="'operation-' + i" autocomplete="off" [ngModel]="offer.operation" (ngModelChange)="update(i,'operation',$event)" placeholder="Ex.: crédito pessoal" /></label>
            <div class="fields two">
              <label>Valor solicitado (R$)<input type="number" min="0" step="0.01" [name]="'amount-' + i" [ngModel]="offer.amount" (ngModelChange)="update(i,'amount',$event)" /></label>
              <label>Entrada (R$)<input type="number" min="0" step="0.01" [name]="'down-' + i" [ngModel]="offer.downPayment" (ngModelChange)="update(i,'downPayment',$event)" /></label>
              <label>Juros ao mês (%)<input type="number" min="0" step="0.001" [name]="'monthly-' + i" [ngModel]="offer.monthlyRate" (ngModelChange)="update(i,'monthlyRate',$event)" /></label>
              <label>Juros ao ano (%)<input type="number" min="0" step="0.001" [name]="'annual-' + i" [ngModel]="offer.annualRate" (ngModelChange)="update(i,'annualRate',$event)" /></label>
              <label>CET ao ano (%)<input type="number" min="0" step="0.001" [name]="'cet-' + i" [ngModel]="offer.cet" (ngModelChange)="update(i,'cet',$event)" /></label>
              <label>Número de parcelas<input type="number" min="1" step="1" [name]="'count-' + i" [ngModel]="offer.installments" (ngModelChange)="update(i,'installments',$event)" /></label>
              <label>Valor de cada parcela (R$)<input type="number" min="0" step="0.01" [name]="'installment-' + i" [ngModel]="offer.installmentValue" (ngModelChange)="update(i,'installmentValue',$event)" /></label>
            </div>
            <details><summary>Custos discriminados</summary>
              <p class="hint">Se esses itens já estiverem embutidos nas parcelas, mantenha a opção marcada para não contar duas vezes.</p>
              <label class="check"><input type="checkbox" [name]="'included-' + i" [ngModel]="offer.chargesIncluded" (ngModelChange)="update(i,'chargesIncluded',$event)" /> IOF, tarifas, seguros e outros encargos já estão incluídos nas parcelas</label>
              <div class="fields two">
                <label>IOF (R$)<input type="number" min="0" step="0.01" [name]="'iof-' + i" [ngModel]="offer.iof" (ngModelChange)="update(i,'iof',$event)" /></label>
                <label>Tarifas administrativas (R$)<input type="number" min="0" step="0.01" [name]="'fees-' + i" [ngModel]="offer.fees" (ngModelChange)="update(i,'fees',$event)" /></label>
                <label>Seguros (R$)<input type="number" min="0" step="0.01" [name]="'insurance-' + i" [ngModel]="offer.insurance" (ngModelChange)="update(i,'insurance',$event)" /></label>
                <label>Demais encargos (R$)<input type="number" min="0" step="0.01" [name]="'other-' + i" [ngModel]="offer.otherCharges" (ngModelChange)="update(i,'otherCharges',$event)" /></label>
              </div>
            </details>
          </fieldset>
        }
      </div>

      <section class="comparison" aria-labelledby="comparison-title">
        <div class="section-head"><div><span class="eyebrow">RESULTADO</span><h2 id="comparison-title">Comparação</h2></div><span>{{ completeOffers().length }} proposta(s) completas</span></div>
        @if (invalidOffers().length) { <p class="validation-note" role="status">{{ invalidOffers().length }} proposta(s) têm valores negativos ou entrada maior que o valor solicitado. Corrija esses campos para incluí-las na comparação.</p> }
        @if (completeOffers().length < 2) {
          <p class="hint" role="status">Preencha valor solicitado, quantidade de parcelas e valor da parcela em pelo menos duas propostas para comparar.</p>
        } @else {
          @if (!sameConditions()) { <p class="validation-note" role="note">As propostas têm valores, modalidades ou prazos diferentes. Os totais não representam comparação equivalente; use “Criar cópias com mesmo principal” para simular o mesmo valor sem alterar os originais.</p> }
          <div class="highlights">
            <article><span>Menor custo total informado</span><strong>{{ lowestTotal()!.offer.institution || 'Proposta ' + (lowestTotal()!.index + 1) }}</strong><small>{{ lowestTotal()!.total | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</small></article>
            <article><span>Menor CET informado</span>
              @if (lowestCet(); as cetOffer) { <strong>{{ cetOffer.offer.institution || 'Proposta ' + (cetOffer.index + 1) }}</strong><small>{{ cetOffer.offer.cet | number:'1.2-3':'pt-BR' }}% a.a.</small> }
              @else { <strong>Não informado</strong><small>Informe o CET nas propostas</small> }
            </article>
            <article><span>Menor parcela informada</span><strong>{{ lowestInstallment()!.offer.institution || 'Proposta ' + (lowestInstallment()!.index + 1) }}</strong><small>{{ lowestInstallment()!.offer.installmentValue | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</small></article>
          </div>
          <div class="table-wrap"><table><caption>Custos calculados a partir dos valores informados</caption><thead><tr><th scope="col">Critério</th>@for (item of completeOffers(); track item.index) { <th scope="col">{{ item.offer.institution || 'Proposta ' + (item.index + 1) }}</th> }</tr></thead>
            <tbody>
              <tr><th scope="row">Operação</th>@for (item of completeOffers(); track item.index) { <td>{{ item.offer.operation || 'Não informado' }}</td> }</tr>
              <tr><th scope="row">Valor solicitado</th>@for (item of completeOffers(); track item.index) { <td>{{ item.offer.amount | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td> }</tr>
              <tr><th scope="row">Custo das parcelas</th>@for (item of completeOffers(); track item.index) { <td>{{ item.installmentTotal | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td> }</tr><tr><th scope="row">Custo excedente estimado sobre principal líquido</th>@for (item of completeOffers(); track item.index) { <td>{{ interestEstimate(item.offer) | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td> }</tr>
              <tr><th scope="row">Entrada + custos extras não incluídos</th>@for (item of completeOffers(); track item.index) { <td>{{ item.upfrontTotal | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td> }</tr>
              <tr class="total"><th scope="row">Total desembolsado estimado</th>@for (item of completeOffers(); track item.index) { <td>{{ item.total | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td> }</tr>
              <tr><th scope="row">CET anual (informado ou estimado)</th>@for (item of completeOffers(); track item.index) { <td>{{ cetAnnual(item.offer) }}</td> }</tr>
              <tr><th scope="row">Juros mensal / anual</th>@for (item of completeOffers(); track item.index) { <td>{{ rate(item.offer.monthlyRate) }} / {{ rate(item.offer.annualRate) }}</td> }</tr>
              <tr><th scope="row">Prazo</th>@for (item of completeOffers(); track item.index) { <td>{{ item.offer.installments }} parcelas</td> }</tr>
              <tr><th scope="row">Economia potencial vs. maior total</th>@for (item of completeOffers(); track item.index) { <td>{{ (maximumTotal() - item.total) | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}</td> }</tr>
            </tbody></table></div>
          <div class="bars" aria-label="Gráfico comparativo do total estimado">
            @for (item of completeOffers(); track item.index) { <div class="bar-row"><span>{{ item.offer.institution || 'Proposta ' + (item.index + 1) }}</span><div class="bar-track"><div class="bar" [style.width.%]="barWidth(item.total)"></div></div><strong>{{ item.total | currency:'BRL':'symbol':'1.0-0':'pt-BR' }}</strong></div> }
          </div>
        }
      </section>

      <p class="disclaimer">O total estimado soma entrada e parcelas; encargos discriminados são somados apenas quando você indica que estão fora das parcelas. Confira o contrato, a memória de cálculo do CET e a composição dos valores com a instituição. Menor parcela não significa menor custo.</p>
    </section>
  `,
  styles: [`
    .proposal-page{max-width:1180px}.proposal-page>.lead{max-width:72ch;line-height:1.6}.privacy-note{margin:24px 0;padding:16px 20px;border:1px solid #b7d9e9;border-radius:10px;background:#eff8fc;line-height:1.5}.market-reference{display:flex;justify-content:space-between;align-items:center;gap:20px;margin:20px 0 30px;padding:20px;border:1px solid var(--line);border-radius:var(--radius);background:var(--surface)}.market-reference h2{margin:4px 0;font:500 23px Georgia,serif;color:var(--navy)}.market-reference p{margin:6px 0;color:var(--muted)}.market-reference small{color:var(--muted)}.market-reference a{color:var(--blue);font-weight:700}.market-value{font-size:24px;font-weight:800;color:var(--navy)!important}.proposal-actions{display:flex;align-items:center;justify-content:space-between;margin:28px 0 16px;color:var(--muted);font-weight:700}.secondary{border:1px solid var(--line);border-radius:8px;padding:11px 16px;background:white;color:var(--navy);font-weight:700;cursor:pointer}.proposal-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.offer-card{position:relative;min-width:0;margin:0;padding:24px;border:1px solid var(--line);border-radius:var(--radius);background:white;box-shadow:var(--shadow)}.offer-card legend{padding:0 8px;font:500 23px Georgia,serif;color:var(--navy)}.offer-card label{margin:14px 0;font-size:13px}.offer-card input:not([type=checkbox]){display:block;width:100%;min-height:42px;margin-top:6px;padding:9px 10px;border:1px solid #bccbd3;border-radius:7px;background:#fff;color:var(--ink)}.fields{display:grid;gap:0 12px}.fields.two{grid-template-columns:repeat(2,minmax(0,1fr))}.check{display:flex!important;align-items:flex-start;gap:9px;line-height:1.45}.check input{margin-top:3px}.remove{position:absolute;right:20px;top:10px;border:0;background:none;color:var(--danger);text-decoration:underline;cursor:pointer}.offer-card details{margin-top:12px;padding-top:12px;border-top:1px solid var(--line)}.offer-card summary{cursor:pointer;font-weight:750;color:var(--blue)}.hint{color:var(--muted);line-height:1.55}.validation-note{padding:12px;border-left:3px solid var(--danger);background:#fff5f5;color:var(--danger)}.comparison{margin-top:58px}.comparison h2{margin:4px 0;font:500 32px Georgia,serif}.highlights{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px;margin:18px 0}.highlights article{display:grid;gap:8px;padding:19px;border:1px solid var(--line);border-radius:10px;background:var(--surface)}.highlights span,.highlights small{color:var(--muted)}.highlights strong{font-size:18px;color:var(--navy)}.table-wrap{overflow:auto;border:1px solid var(--line);border-radius:10px}table{width:100%;min-width:650px;border-collapse:collapse;background:#fff}caption{text-align:left;padding:15px;color:var(--muted)}th,td{text-align:left;padding:12px 15px;border-top:1px solid var(--line);white-space:nowrap}thead th{background:var(--surface)}tbody th{font-weight:700}.total{background:#f2f8f5;font-weight:800}.bars{display:grid;gap:12px;margin:20px 0}.bar-row{display:grid;grid-template-columns:minmax(120px,180px) 1fr auto;align-items:center;gap:12px;font-size:13px}.bar-track{height:12px;overflow:hidden;border-radius:99px;background:var(--surface)}.bar{height:100%;border-radius:99px;background:var(--blue-2)}.disclaimer{margin:24px 0;color:var(--muted);font-size:13px;line-height:1.6}@media(max-width:760px){.proposal-grid,.highlights{grid-template-columns:1fr}.fields.two{grid-template-columns:1fr 1fr}.market-reference{align-items:flex-start;flex-direction:column}}@media(max-width:480px){.fields.two{grid-template-columns:1fr}.bar-row{grid-template-columns:1fr auto}.bar-track{grid-column:1/-1;grid-row:2}}
  `]
})
export class ProposalComparatorComponent {
  private readonly http = inject(HttpClient);
  readonly offers = signal<Proposal[]>([newProposal(), newProposal()]);
  readonly marketRate = signal<{annualRatePercent:number;referenceMonth:string;retrievedAt:string}|null>(null);
  readonly marketLoading = signal(true);
  readonly catalog = signal<MarketCatalog | null>(null);
  readonly catalogLoading = signal(true);
  readonly selectedModality = signal('');
  readonly bankSearch = signal('');
  readonly visibleRates = computed(() => (this.catalog()?.rates ?? []).filter(x => x.institution.toLocaleLowerCase('pt-BR').includes(this.bankSearch().toLocaleLowerCase('pt-BR'))).sort((a,b) => a.monthlyRatePercent-b.monthlyRatePercent));
  readonly completeOffers = computed(() => this.offers().map((offer, index) => ({ offer, index, ...this.cost(offer) }))
    .filter(item => !this.isInvalid(item.offer) && item.offer.amount !== null && item.offer.amount > 0 && item.offer.installments !== null && item.offer.installments > 0 && Number.isInteger(item.offer.installments) && item.offer.installmentValue !== null && item.offer.installmentValue > 0));
  readonly invalidOffers = computed(() => this.offers().filter(offer => this.isInvalid(offer)));
  readonly lowestTotal = computed(() => [...this.completeOffers()].sort((a, b) => a.total - b.total).at(0) ?? null);
  readonly lowestInstallment = computed(() => [...this.completeOffers()].sort((a, b) => (a.offer.installmentValue ?? Infinity) - (b.offer.installmentValue ?? Infinity)).at(0) ?? null);
  readonly lowestCet = computed(() => [...this.completeOffers()].filter(x => x.offer.cet !== null && x.offer.cet >= 0).sort((a, b) => a.offer.cet! - b.offer.cet!).at(0) ?? null);
  readonly maximumTotal = computed(() => Math.max(0, ...this.completeOffers().map(x => x.total)));
  readonly sameConditions = computed(() => { const rows=this.completeOffers(); return rows.length<2 || rows.every(x=>x.offer.amount===rows[0].offer.amount && x.offer.installments===rows[0].offer.installments && (!rows[0].offer.operation || !x.offer.operation || x.offer.operation.toLowerCase()===rows[0].offer.operation.toLowerCase())); });

  constructor() {
    this.http.get<{annualRatePercent:number;referenceMonth:string;retrievedAt:string}>('/api/v1/market-rates/personal-credit').subscribe({
      next: value => { this.marketRate.set(value); this.marketLoading.set(false); },
      error: () => this.marketLoading.set(false)
    });
    this.http.get<MarketCatalog>('/api/v1/market-rates/credit-institutions').subscribe({ next: value => { this.catalog.set(value); this.catalogLoading.set(false); }, error: () => this.catalogLoading.set(false) });
  }

  loadModality(value: string): void {
    this.selectedModality.set(value);
    if (!value) { this.catalog.update(data => data ? { ...data, rates: [] } : null); return; }
    this.catalogLoading.set(true);
    this.http.get<MarketCatalog>('/api/v1/market-rates/credit-institutions', { params: { modality: value } }).subscribe({ next: data => { this.catalog.set(data); this.catalogLoading.set(false); }, error: () => this.catalogLoading.set(false) });
  }
  useRate(row: MarketCatalog['rates'][number]): void {
    const empty = this.offers().findIndex(x => !x.institution && !x.amount);
    const index = empty >= 0 ? empty : this.offers().length - 1;
    const offer=this.offers()[index];
    this.update(index, 'institution', row.institution);
    this.update(index, 'operation', row.modality);
    this.update(index, 'monthlyRate', row.monthlyRatePercent);
    this.update(index, 'annualRate', row.annualRatePercent);
    if(offer.amount && offer.installments && !offer.installmentValue){const principal=Math.max(0,offer.amount-(offer.downPayment??0));const rate=row.monthlyRatePercent/100;const payment=rate===0?principal/offer.installments:principal*rate/(1-Math.pow(1+rate,-offer.installments));this.update(index,'installmentValue',Math.round(payment*100)/100);}
  }
  duplicateOffer(): void { const last = this.offers().at(-1); if (last) this.offers.update(items => [...items, { ...last }]); }
  normalizeAmounts(): void { const rows=this.completeOffers(); if(rows.length<2)return; const principal=rows[0].offer.amount!; this.offers.update(items=>[...items,...rows.map(x=>({...x.offer,institution:`${x.offer.institution||`Proposta ${x.index+1}`} · mesmo principal`,amount:principal,downPayment:(x.offer.downPayment??0)>principal?0:x.offer.downPayment}))]); }
  clearOffers(): void { this.offers.set([newProposal(), newProposal()]); }

  update<K extends keyof Proposal>(index: number, field: K, value: Proposal[K]): void {
    this.offers.update(items => items.map((item, i) => i === index ? { ...item, [field]: value } as Proposal : item));
  }
  addOffer(): void { this.offers.update(items => [...items, newProposal()]); }
  removeOffer(index: number): void { if (this.offers().length > 2) this.offers.update(items => items.filter((_, i) => i !== index)); }
  rate(value: number | null): string { return value === null ? '—' : `${value.toLocaleString('pt-BR', { maximumFractionDigits: 3 })}%`; }
  interestEstimate(offer: Proposal): number { return Math.max(0,(offer.installments??0)*(offer.installmentValue??0)-Math.max(0,(offer.amount??0)-(offer.downPayment??0))); }
  cetAnnual(offer: Proposal): string {
    if (offer.cet !== null) return `${offer.cet.toLocaleString('pt-BR',{maximumFractionDigits:3})}% a.a. (informado)`;
    if (offer.monthlyRate === null || offer.installments === null || offer.installmentValue === null || !offer.amount) return 'Não informado';
    const net=offer.amount-(offer.chargesIncluded?0:(offer.iof??0)+(offer.fees??0)+(offer.insurance??0)+(offer.otherCharges??0)); if(net<=0)return 'Não estimável';
    let low=-0.99,high=10;for(let i=0;i<100;i++){const mid=(low+high)/2;const pv=mid===0?offer.installmentValue*offer.installments:offer.installmentValue*(1-Math.pow(1+mid,-offer.installments))/mid;if(pv>net)low=mid;else high=mid;}
    return `${((Math.pow(1+(low+high)/2,12)-1)*100).toLocaleString('pt-BR',{maximumFractionDigits:3})}% a.a. (estimativa)`;
  }
  barWidth(total: number): number { const max = this.maximumTotal(); return max === 0 ? 0 : Math.max(2, total / max * 100); }

  private isInvalid(offer: Proposal): boolean {
    const numeric = [offer.amount, offer.downPayment, offer.monthlyRate, offer.annualRate, offer.cet, offer.installments, offer.installmentValue, offer.iof, offer.fees, offer.insurance, offer.otherCharges];
    return numeric.some(value => value !== null && value < 0) || (offer.amount !== null && offer.downPayment !== null && offer.downPayment > offer.amount);
  }

  private cost(offer: Proposal): { installmentTotal: number; upfrontTotal: number; total: number } {
    const installmentTotal = (offer.installments ?? 0) * (offer.installmentValue ?? 0);
    const extras = offer.chargesIncluded ? 0 : (offer.iof ?? 0) + (offer.fees ?? 0) + (offer.insurance ?? 0) + (offer.otherCharges ?? 0);
    const upfrontTotal = (offer.downPayment ?? 0) + extras;
    return { installmentTotal, upfrontTotal, total: installmentTotal + upfrontTotal };
  }
}
