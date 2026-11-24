import { Component, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';

interface ToolCard { slug: string; title: string; summary: string; group: string; }

@Component({
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="page calculator-center" aria-labelledby="calculator-center-title">
      <span class="eyebrow">SIMULADORES EDUCATIVOS</span>
      <h1 id="calculator-center-title">Calculadoras financeiras</h1>
      <p class="lead">Explore cenários com os valores e as taxas que você informar. Cada simulação apresenta a fórmula e as premissas utilizadas.</p>
      <p class="notice">Resultados estimativos. Não incluem impostos ou custos que não sejam informados e não substituem o valor oficial de contratos ou instituições.</p>
      @for (group of groups; track group) {
        <section class="tool-group" [attr.aria-label]="group">
          <h2>{{ group }}</h2>
          <div class="tool-grid">
            @for (tool of tools.filter(t => t.group === group); track tool.slug) {
              <a class="tool-card" [routerLink]="['/calculadoras', tool.slug]"><span class="eyebrow">{{ tool.group }}</span><h3>{{ tool.title }}</h3><p>{{ tool.summary }}</p><strong>Abrir calculadora →</strong></a>
            }
          </div>
        </section>
      }
      <p class="source-note">A Calculadora do Cidadão do Banco Central também é uma referência pública: <a href="https://www.bcb.gov.br/meubc/calculadoradocidadao" target="_blank" rel="noopener noreferrer">bcb.gov.br/meubc/calculadoradocidadao ↗</a></p>
    </section>
  `,
  styles: [`
    .calculator-center{max-width:1180px}.calculator-center>.lead{max-width:70ch;line-height:1.6}.notice{margin:22px 0;padding:16px;border-left:3px solid var(--blue-2);background:var(--surface);color:var(--muted);line-height:1.5}.tool-group{margin:42px 0}.tool-group h2{font:500 30px Georgia,serif;color:var(--navy)}.tool-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}.tool-card{display:flex;flex-direction:column;min-height:200px;padding:22px;border:1px solid var(--line);border-radius:var(--radius);background:#fff;transition:box-shadow .2s,border-color .2s}.tool-card:hover,.tool-card:focus-visible{border-color:var(--blue-2);box-shadow:var(--shadow)}.tool-card h3{margin:10px 0 4px;font:500 22px Georgia,serif;color:var(--navy)}.tool-card p{color:var(--muted);line-height:1.5}.tool-card strong{margin-top:auto;padding-top:12px;color:var(--blue)}.source-note{margin-top:45px;color:var(--muted)}.source-note a{color:var(--blue);text-decoration:underline}@media(max-width:850px){.tool-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:520px){.tool-grid{grid-template-columns:1fr}}
  `]
})
export class CalculatorCenterComponent {
  constructor() { inject(Meta).updateTag({ name: 'description', content: 'Calculadoras financeiras educativas: juros, financiamentos, investimentos, planejamento mensal, dívidas e CET, com fórmulas e premissas explicadas.' }); }
  readonly groups = ['Juros e financiamentos', 'Investimentos e patrimônio', 'Organização financeira', 'Crédito e dívidas'];
  readonly tools: ToolCard[] = [
    {slug:'juros-simples',title:'Juros simples',summary:'Calcule juros proporcionais ao capital e ao prazo.',group:'Juros e financiamentos'},
    {slug:'juros-compostos',title:'Juros compostos',summary:'Projete capital inicial com taxa mensal e prazo informados.',group:'Juros e financiamentos'},
    {slug:'financiamento-fixas',title:'Parcelas fixas',summary:'Estime parcela, total pago e juros por pagamentos constantes.',group:'Juros e financiamentos'},
    {slug:'financiamento-sac',title:'Financiamento SAC',summary:'Veja amortização constante e a trajetória decrescente das parcelas.',group:'Juros e financiamentos'},
    {slug:'financiamento-price',title:'Financiamento Price',summary:'Estime parcelas constantes e confira a tabela de amortização.',group:'Juros e financiamentos'},
    {slug:'simulacao-emprestimo',title:'Simulação de empréstimo',summary:'Inclua custos financiados e compare o total estimado.',group:'Juros e financiamentos'},
    {slug:'investimento-aportes',title:'Aplicação com aportes',summary:'Simule depósitos mensais no fim de cada período.',group:'Investimentos e patrimônio'},
    {slug:'rentabilidade-investimento',title:'Rentabilidade',summary:'Calcule ganho acumulado e taxa anualizada entre dois valores.',group:'Investimentos e patrimônio'},
    {slug:'correcao-monetaria',title:'Correção monetária',summary:'Aplique a variação acumulada de um índice que você informar.',group:'Investimentos e patrimônio'},
    {slug:'reserva-emergencia',title:'Reserva de emergência',summary:'Calcule um alvo a partir de despesas e meses definidos por você.',group:'Organização financeira'},
    {slug:'planejamento-mensal',title:'Planejamento mensal',summary:'Organize renda, despesas, dívidas e meta de poupança.',group:'Organização financeira'},
    {slug:'quitacao-antecipada',title:'Quitação antecipada',summary:'Estime o valor presente de parcelas futuras.',group:'Crédito e dívidas'},
    {slug:'portabilidade-divida',title:'Portabilidade de dívida',summary:'Compare parcelas e custo estimado no mesmo prazo.',group:'Crédito e dívidas'},
    {slug:'consorcio-financiamento',title:'Consórcio x financiamento',summary:'Compare custos simplificados com taxas informadas.',group:'Crédito e dívidas'},
    {slug:'cet',title:'Custo Efetivo Total (CET)',summary:'Estime a taxa implícita nas parcelas e custos iniciais informados.',group:'Crédito e dívidas'},
    {slug:'ferias',title:'Férias',summary:'Consulte uma estimativa bruta de remuneração e abono.',group:'Organização financeira'},
    {slug:'decimo-terceiro',title:'13º salário',summary:'Estime o valor bruto proporcional aos meses trabalhados.',group:'Organização financeira'}
  ];
}
