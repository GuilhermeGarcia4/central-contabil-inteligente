import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CategorySlice } from './finance.service';
import { chartColor } from './financial-chart-palette';

@Component({
  selector:'app-donut-chart', imports:[CurrencyPipe,DecimalPipe],
  template:`<section class="donut-card" [attr.aria-label]="title">
    <h2>{{title}}</h2>
    @if(displaySegments.length){
      <div class="donut-layout">
        <div class="donut" role="img" [attr.aria-label]="accessibleSummary" [style.background]="gradient"><div><strong>{{total|currency:'BRL':'symbol':'1.2-2':'pt-BR'}}</strong><span>{{centerLabel}}</span></div></div>
        <ul aria-label="Legenda do gráfico">@for(item of displaySegments;track item.categoryId){<li [title]="item.categoryName+' · '+(item.amount|currency:'BRL':'symbol':'1.2-2':'pt-BR')+' · '+(item.percentage|number:'1.1-1':'pt-BR')+'%'"><i [style.background]="item.resolvedColor"></i><button type="button" (click)="categorySelected.emit(item.categoryId)" [attr.aria-label]="'Ver lançamentos de '+item.categoryName"><span><b>{{item.categoryName}}</b><small>{{item.percentage|number:'1.1-1':'pt-BR'}}%</small></span><strong>{{item.amount|currency:'BRL':'symbol':'1.2-2':'pt-BR'}}</strong></button></li>}</ul>
      </div>
    }@else{<p class="chart-empty">{{emptyMessage}}</p>}
  </section>`,
  styles:[`.donut-card{border:1px solid var(--line);border-radius:var(--radius);padding:24px;background:#fff;height:100%}.donut-card h2{margin-top:0}.donut-layout{display:grid;grid-template-columns:190px 1fr;gap:28px;align-items:center}.donut{width:180px;aspect-ratio:1;border-radius:50%;display:grid;place-items:center}.donut>div{width:58%;aspect-ratio:1;border-radius:50%;background:#fff;display:grid;place-content:center;text-align:center;box-shadow:inset 0 0 0 1px var(--line)}.donut strong{font-size:15px}.donut span{font-size:11px;color:var(--muted)}ul{list-style:none;margin:0;padding:0;display:grid;gap:10px}li{display:grid;grid-template-columns:10px 1fr;gap:9px;align-items:center}li i{width:10px;height:10px;border-radius:50%}li button{border:0;background:none;padding:4px;text-align:left;display:grid;grid-template-columns:1fr auto;gap:10px;cursor:pointer;border-radius:6px}li button:hover,li button:focus-visible{background:var(--surface);outline:2px solid var(--blue)}li span{display:grid}li small{color:var(--muted)}.chart-empty{min-height:170px;display:grid;place-items:center;text-align:center;color:var(--muted)}@media(max-width:600px){.donut-layout{grid-template-columns:1fr}.donut{margin:auto}}`]
})
export class DonutChartComponent{
  @Input() title='';@Input() centerLabel='Total';@Input() segments:CategorySlice[]=[];@Input() emptyMessage='Nenhum dado para exibir.';@Output() categorySelected=new EventEmitter<string>();
  get displaySegments(){const valid=(this.segments??[]).filter(item=>Number.isFinite(Number(item.amount))&&Number(item.amount)>0);const total=valid.reduce((sum,item)=>sum+Number(item.amount),0);return total<=0?[]:valid.map((item,index)=>({...item,amount:Number(item.amount),percentage:Number(item.amount)/total*100,resolvedColor:chartColor(item.color,index)}))}
  get total(){return this.displaySegments.reduce((sum,item)=>sum+item.amount,0)}
  get gradient(){const items=this.displaySegments;if(!items.length)return 'conic-gradient(#ece5e9 0 100%)';let current=0;const parts=items.map((item,index)=>{const start=current;current+=item.percentage;const end=index===items.length-1?100:Math.min(current,100);return `${item.resolvedColor} ${start}% ${end}%`});return `conic-gradient(${parts.join(',')})`}
  get accessibleSummary(){return `${this.title}. ${this.displaySegments.map(x=>`${x.categoryName}: ${x.amount.toLocaleString('pt-BR',{style:'currency',currency:'BRL'})}, ${x.percentage.toFixed(1)}%`).join('; ')}`}
}
