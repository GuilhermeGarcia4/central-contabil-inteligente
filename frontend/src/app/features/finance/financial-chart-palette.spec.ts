import { chartColor, contrastText, FINANCIAL_CHART_PALETTE } from './financial-chart-palette';

describe('FinancialChartPalette',()=>{
  it('usa cor personalizada válida e fallback centralizado',()=>{expect(chartColor('#8A1455',0)).toBe('#8A1455');expect(chartColor(undefined,1)).toBe(FINANCIAL_CHART_PALETTE[1]);expect(chartColor('red',2)).toBe(FINANCIAL_CHART_PALETTE[2])});
  it('escolhe texto com contraste',()=>{expect(contrastText('#660240')).toBe('#FFFFFF');expect(contrastText('#FFFDF8')).toBe('#341522')});
});
