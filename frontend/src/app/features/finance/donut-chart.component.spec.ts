import { DonutChartComponent } from './donut-chart.component';

describe('DonutChartComponent',()=>{
  it('calcula total, gradiente e descrição acessível',()=>{const component=new DonutChartComponent();component.title='Entradas';component.segments=[{categoryId:'1',categoryName:'Salário',amount:4500,percentage:100,color:'#8A1455'}];expect(component.total).toBe(4500);expect(component.gradient).toContain('#8A1455 0% 100%');expect(component.accessibleSummary).toContain('100.0%');expect(component.accessibleSummary).toContain('4.500,00')});
  it('permanece válido sem segmentos',()=>{const component=new DonutChartComponent();expect(component.total).toBe(0);expect(component.accessibleSummary).toBe('. ')});
  it('ignora valores inválidos e recalcula percentuais pelo valor real',()=>{const component=new DonutChartComponent();component.segments=[{categoryId:'1',categoryName:'A',amount:1,percentage:999},{categoryId:'2',categoryName:'B',amount:3,percentage:-10},{categoryId:'3',categoryName:'Inválido',amount:Number.NaN,percentage:0}];expect(component.displaySegments.map(x=>x.percentage)).toEqual([25,75]);expect(component.gradient).toContain('25% 100%')});
});
