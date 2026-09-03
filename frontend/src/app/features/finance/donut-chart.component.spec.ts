import { DonutChartComponent } from './donut-chart.component';

describe('DonutChartComponent',()=>{
  it('calcula total e gradiente para uma única categoria',()=>{const component=new DonutChartComponent();component.title='Receitas';component.segments=[{categoryId:'1',categoryName:'Salário',amount:4500,percentage:100}];expect(component.total).toBe(4500);expect(component.gradient).toContain('0% 100%');expect(component.accessibleSummary).toContain('Salário: 100.0%')});
  it('permanece válido sem segmentos',()=>{const component=new DonutChartComponent();expect(component.total).toBe(0);expect(component.accessibleSummary).toBe('. ')});
  it('ignora valores inválidos e recalcula percentuais pelo valor real',()=>{const component=new DonutChartComponent();component.segments=[{categoryId:'1',categoryName:'A',amount:1,percentage:999},{categoryId:'2',categoryName:'B',amount:3,percentage:-10},{categoryId:'3',categoryName:'Inválido',amount:Number.NaN,percentage:0}];expect(component.displaySegments.map(x=>x.percentage)).toEqual([25,75]);expect(component.gradient).toContain('25% 100%')});
});
