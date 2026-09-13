import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FinanceService } from './finance.service';

describe('FinanceService V8',()=>{
  let service:FinanceService;let http:HttpTestingController;
  beforeEach(()=>{TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});service=TestBed.inject(FinanceService);http=TestBed.inject(HttpTestingController)});
  afterEach(()=>http.verify());
  it('consulta overview por intervalo sem enviar userId',()=>{service.overview('2026-07-01','2026-09-30').subscribe(value=>expect(value.totalEntries).toBe(100));const request=http.expectOne('/api/v1/finance/overview?startDate=2026-07-01&endDate=2026-09-30');expect(request.request.method).toBe('GET');expect(request.request.params.has('userId')).toBe(false);request.flush({totalEntries:100})});
  it('salva e restaura cor sem enviar userId',()=>{service.saveCategoryColor('c1','#8A1455').subscribe();const put=http.expectOne('/api/v1/finance/categories/c1/chart-color');expect(put.request.body).toEqual({chartColor:'#8A1455'});expect(put.request.body.userId).toBeUndefined();put.flush({categoryId:'c1',chartColor:'#8A1455',hasCustomColor:true});service.restoreCategoryColor('c1').subscribe();const remove=http.expectOne('/api/v1/finance/categories/c1/chart-color');expect(remove.request.method).toBe('DELETE');remove.flush({categoryId:'c1',chartColor:'#660240',hasCustomColor:false})});
});
