export const FINANCIAL_CHART_PALETTE=[
  '#660240','#B51F72','#D9578B','#8A1455','#C96B98','#6B5B95','#397367',
  '#C17C24','#355C7D','#A23B72','#4F6D7A','#8C6A43','#7A4EAB','#2E7D6E'
] as const;

export function chartColor(color:string|undefined,index:number){
  return /^#[0-9A-F]{6}$/i.test(color??'')?color!:FINANCIAL_CHART_PALETTE[index%FINANCIAL_CHART_PALETTE.length];
}

export function contrastText(hex:string){
  const value=hex.replace('#','');
  const red=parseInt(value.slice(0,2),16);const green=parseInt(value.slice(2,4),16);const blue=parseInt(value.slice(4,6),16);
  return (red*299+green*587+blue*114)/1000>=150?'#341522':'#FFFFFF';
}
