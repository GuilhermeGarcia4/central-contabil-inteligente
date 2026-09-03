import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { FINANCE_PAYMENT_LABELS, FINANCE_TRANSACTION_LABELS } from './finance-labels';

export type TransactionType = 'Income'|'Expense';
export type PaymentMethod = 'Cash'|'Pix'|'DebitCard'|'CreditCard'|'BankTransfer'|'Boleto'|'Other';
export interface FinanceCategory { id:string; name:string; type:TransactionType; icon:string|null; isDefault:boolean; isCustom:boolean }
export interface CategorySlice { categoryId:string; categoryName:string; amount:number; percentage:number }
export interface FinanceTransaction { id:string; type:TransactionType; categoryId:string; categoryName:string; description:string; amount:number; transactionDate:string; paymentMethod:PaymentMethod; isRecurring:boolean; recurrenceId:string|null; installmentPlanId:string|null; installmentNumber:number|null; installmentCount:number|null; notes:string|null; createdAt:string }
export interface FinanceSummary { year:number; month:number; totalIncome:number; totalExpenses:number; balance:number; incomeCommitmentPercentage:number|null; transactionCount:number; expensesByCategory:CategorySlice[]; incomeByCategory:CategorySlice[]; comparison:{incomePercentage:number|null;expensePercentage:number|null;balancePercentage:number|null}; latestTransactions:FinanceTransaction[] }
export interface TransactionPayload { type:TransactionType; categoryId:string; description:string; amount:number; transactionDate:string; paymentMethod:PaymentMethod; isRecurring:boolean; recurrenceEndDate:string|null; notes:string|null; isInstallment:boolean; installmentCount:number|null; firstInstallmentDate:string|null; creditCardId:string|null; accountId:string|null }
export interface Suggestion { categoryId:string; categoryName:string; confidence:number; source:string }
export interface BudgetProgress { id:string;categoryId:string;categoryName:string;plannedAmount:number;usedAmount:number;remainingAmount:number;percentage:number;isExceeded:boolean }
export interface PlanningSummary {year:number;month:number;totalPlanned:number;totalUsed:number;available:number;categories:BudgetProgress[]}
export interface GoalContribution {id:string;amount:number;date:string;description:string|null;createdAt:string}
export interface FinancialGoal {id:string;name:string;targetAmount:number;currentAmount:number;targetDate:string|null;description:string|null;isCompleted:boolean;progressPercentage:number;contributions:GoalContribution[]}
export interface InstallmentPlan {id:string;description:string;categoryId:string;categoryName:string;totalAmount:number;installmentCount:number;firstDueDate:string;elapsedInstallments:number;remainingAmount:number;nextDueDate:string|null}
export interface Forecast {year:number;month:number;expectedIncome:number;expectedExpenses:number;expectedBalance:number;disclaimer:string}
export interface MonthlyReport {year:number;month:number;totalIncome:number;totalExpenses:number;balance:number;savingsRate:number|null;largestExpenseCategory:CategorySlice|null;budget:PlanningSummary;alerts:string[];insight:string}
export type AccountType='Checking'|'Digital'|'Cash'|'Wallet'|'Savings';
export type ScheduledStatus='Pending'|'Paid'|'Received';
export interface CreditCard {id:string;name:string;bank:string|null;limit:number;closingDay:number;dueDay:number;last4:string|null;color:string|null;isActive:boolean}
export interface Invoice {cardId:string;cardName:string;cardColor:string|null;year:number;month:number;value:number;limit:number;limitUsed:number;limitAvailable:number;closingDate:string;dueDate:string;transactionCount:number}
export interface FinancialAccount {id:string;name:string;type:AccountType;balance:number;color:string|null;isActive:boolean}
export interface ScheduledTransaction {id:string;type:TransactionType;categoryId:string;categoryName:string;description:string;amount:number;dueDate:string;status:ScheduledStatus;accountId:string|null;accountName:string|null;notes:string|null}
export interface Debt {id:string;name:string;originalAmount:number;currentAmount:number;installmentCount:number|null;interestRate:number|null;dueDate:string|null;institution:string|null;notes:string|null;isPaid:boolean}
export interface Reserve {targetAmount:number;currentAmount:number;targetMonths:number|null;notes:string|null;progressPercentage:number;remaining:number}
export interface NetWorth {assets:number;passives:number;netWorth:number;accounts:FinancialAccount[];reserve:number;debts:number}
export interface CalendarEvent {kind:string;description:string;amount:number;category:string|null;status:string|null}
export interface CalendarDay {date:string;events:CalendarEvent[]}
export interface MonthlySlice {month:number;income:number;expenses:number;balance:number}
export interface AnnualReport {year:number;totalIncome:number;totalExpenses:number;balance:number;months:MonthlySlice[];topCategories:{name:string;amount:number}[]}
export interface MonthUnderstanding { year:number;month:number;totalIncome:number;totalExpenses:number;balance:number;savingsRate:number|null;largestExpenseCategory:string|null;largestExpenseAmount:number|null;comparison:{incomePercentage:number|null;expensePercentage:number|null;balancePercentage:number|null};insights:string[] }
export interface UpcomingBill { dueDate:string;description:string;amount:number;status:string }
export interface WeeklySummary { startDate:string;endDate:string;income:number;expenses:number;balance:number;topCategories:CategorySlice[];upcomingBills:UpcomingBill[];insights:string[] }
export interface GoalProgress { id:string;name:string;targetAmount:number;currentAmount:number;percentage:number }
export interface MonthlySummary { year:number;month:number;income:number;expenses:number;balance:number;largestExpenseCategory:string|null;largestExpenseAmount:number|null;savingsRate:number|null;comparison:{incomePercentage:number|null;expensePercentage:number|null;balancePercentage:number|null};goals:GoalProgress[];insights:string[] }
export interface FinancialAlert { type:string;severity:'danger'|'warning'|'info';message:string;link:string|null }
export type ExplanationProfile='Simple'|'Detailed'|'Both';
export interface UserPreferenceView { explanationProfile:ExplanationProfile;alertBills:boolean;alertInvoices:boolean;alertBudget:boolean;alertGoals:boolean;alertInstallments:boolean;alertWeeklySummary:boolean;alertMonthlySummary:boolean }
export interface ImportPreviewRow { index:number;date:string;description:string;amount:number;type:TransactionType;categoryId:string|null;categoryName:string|null;categoryConfidence:number|null;isDuplicate:boolean;duplicateReason:string|null;status:string }
export interface ImportPreview { fileName:string;total:number;duplicates:number;uncategorized:number;rows:ImportPreviewRow[] }
export interface ImportRowInput { index:number;date:string;description:string;amount:number;type:TransactionType;categoryId:string|null }
export interface ImportResult { imported:number;skippedDuplicates:number;skippedInvalid:number }
export interface ConversationRow { id:string;title:string;createdAt:string;messageCount:number }
export interface ConversationMessage { role:'user'|'assistant';content:string;sourcesJson:string|null;createdAt:string }

@Injectable({providedIn:'root'})
export class FinanceService {
  private readonly http=inject(HttpClient); private readonly base='/api/v1/finance';
  summary(year:number,month:number){return this.http.get<FinanceSummary>(`${this.base}/summary`,{params:{year,month}})}
  categories(){return this.http.get<FinanceCategory[]>(`${this.base}/categories`)}
  createCategory(name:string,type:TransactionType){return this.http.post<FinanceCategory>(`${this.base}/categories`,{name,type,icon:null})}
  updateCategory(id:string,name:string,type:TransactionType){return this.http.put<FinanceCategory>(`${this.base}/categories/${id}`,{name,type,icon:null})}
  removeCategory(id:string){return this.http.delete<void>(`${this.base}/categories/${id}`)}
  transactions(filters:Record<string,string|number|undefined>){let params=new HttpParams();for(const [key,value] of Object.entries(filters))if(value!==undefined&&value!=='')params=params.set(key,String(value));return this.http.get<{items:FinanceTransaction[];total:number;page:number;pageSize:number}>(`${this.base}/transactions`,{params})}
  create(payload:TransactionPayload){return this.http.post<FinanceTransaction>(`${this.base}/transactions`,payload)}
  update(id:string,payload:TransactionPayload){return this.http.put<FinanceTransaction>(`${this.base}/transactions/${id}`,payload)}
  remove(id:string){return this.http.delete<void>(`${this.base}/transactions/${id}`)}
  suggest(type:TransactionType,description:string,notes:string|null){return this.http.post<{suggestions:Suggestion[]}>(`${this.base}/category-suggestion`,{type,description,notes})}
  budgets(year:number,month:number){return this.http.get<PlanningSummary>(`${this.base}/budgets`,{params:{year,month}})}
  saveBudget(categoryId:string,year:number,month:number,plannedAmount:number){return this.http.put(`${this.base}/budgets`,{categoryId,year,month,plannedAmount})}
  removeBudget(id:string){return this.http.delete<void>(`${this.base}/budgets/${id}`)}
  goals(){return this.http.get<FinancialGoal[]>(`${this.base}/goals`)}
  createGoal(body:{name:string;targetAmount:number;currentAmount:number;targetDate:string|null;description:string|null}){return this.http.post<FinancialGoal>(`${this.base}/goals`,body)}
  removeGoal(id:string){return this.http.delete<void>(`${this.base}/goals/${id}`)}
  contribute(goalId:string,amount:number,date:string,description:string|null){return this.http.post(`${this.base}/goals/${goalId}/contributions`,{amount,date,description})}
  installments(){return this.http.get<InstallmentPlan[]>(`${this.base}/installments`)}
  forecast(year:number,month:number){return this.http.get<Forecast>(`${this.base}/forecast`,{params:{year,month}})}
  report(year:number,month:number){return this.http.get<MonthlyReport>(`${this.base}/report`,{params:{year,month}})}
  exportExcel(filters:Record<string,string|number|undefined>){let params=new HttpParams();for(const [key,value] of Object.entries(filters))if(value!==undefined&&value!=='')params=params.set(key,String(value));return this.http.get(`${this.base}/export/excel`,{params,responseType:'blob'})}
  recurrences(){return this.http.get<any[]>(`${this.base}/recurrences`)}
  recurrenceAction(id:string,action:'pause'|'resume'|'end'){return this.http.post(`${this.base}/recurrences/${id}/${action}`,action==='end'?{endDate:new Date().toISOString().slice(0,10)}:{})}
  cards(){return this.http.get<CreditCard[]>(`${this.base}/cards`)}
  createCard(body:{name:string;bank:string|null;limit:number;closingDay:number;dueDay:number;last4:string|null;color:string|null}){return this.http.post<CreditCard>(`${this.base}/cards`,body)}
  updateCard(id:string,body:{name:string;bank:string|null;limit:number;closingDay:number;dueDay:number;last4:string|null;color:string|null}){return this.http.put<CreditCard>(`${this.base}/cards/${id}`,body)}
  removeCard(id:string){return this.http.delete<void>(`${this.base}/cards/${id}`)}
  invoices(year:number,month:number){return this.http.get<Invoice[]>(`${this.base}/cards/invoices`,{params:{year,month}})}
  accounts(){return this.http.get<FinancialAccount[]>(`${this.base}/accounts`)}
  createAccount(body:{name:string;type:AccountType;balance:number;color:string|null}){return this.http.post<FinancialAccount>(`${this.base}/accounts`,body)}
  updateAccount(id:string,body:{name:string;type:AccountType;balance:number;color:string|null}){return this.http.put<FinancialAccount>(`${this.base}/accounts/${id}`,body)}
  removeAccount(id:string){return this.http.delete<void>(`${this.base}/accounts/${id}`)}
  transfer(fromAccountId:string,toAccountId:string,amount:number,date:string,notes:string|null){return this.http.post(`${this.base}/accounts/transfer`,{fromAccountId,toAccountId,amount,date,notes})}
  scheduled(year:number,month:number,status?:ScheduledStatus){let params=new HttpParams().set('year',year).set('month',month);if(status)params=params.set('status',status);return this.http.get<ScheduledTransaction[]>(`${this.base}/scheduled`,{params})}
  createScheduled(body:{type:TransactionType;categoryId:string;description:string;amount:number;dueDate:string;accountId:string|null;notes:string|null}){return this.http.post<ScheduledTransaction>(`${this.base}/scheduled`,body)}
  updateScheduled(id:string,body:{type:TransactionType;categoryId:string;description:string;amount:number;dueDate:string;accountId:string|null;notes:string|null}){return this.http.put<ScheduledTransaction>(`${this.base}/scheduled/${id}`,body)}
  removeScheduled(id:string){return this.http.delete<void>(`${this.base}/scheduled/${id}`)}
  setScheduledStatus(id:string,status:ScheduledStatus){return this.http.post<ScheduledTransaction>(`${this.base}/scheduled/${id}/status`,{status})}
  debts(){return this.http.get<Debt[]>(`${this.base}/debts`)}
  createDebt(body:{name:string;originalAmount:number;currentAmount:number;installmentCount:number|null;interestRate:number|null;dueDate:string|null;institution:string|null;notes:string|null}){return this.http.post<Debt>(`${this.base}/debts`,body)}
  updateDebt(id:string,body:{name:string;originalAmount:number;currentAmount:number;installmentCount:number|null;interestRate:number|null;dueDate:string|null;institution:string|null;notes:string|null}){return this.http.put<Debt>(`${this.base}/debts/${id}`,body)}
  removeDebt(id:string){return this.http.delete<void>(`${this.base}/debts/${id}`)}
  reserve(){return this.http.get<Reserve>(`${this.base}/reserve`)}
  saveReserve(body:{targetAmount:number;currentAmount:number;targetMonths:number|null;notes:string|null}){return this.http.put<Reserve>(`${this.base}/reserve`,body)}
  netWorth(){return this.http.get<NetWorth>(`${this.base}/net-worth`)}
  calendar(year:number,month:number){return this.http.get<CalendarDay[]>(`${this.base}/calendar`,{params:{year,month}})}
  annualReport(year:number){return this.http.get<AnnualReport>(`${this.base}/annual-report`,{params:{year}})}
  understandMonth(year:number,month:number){return this.http.get<MonthUnderstanding>(`${this.base}/understand-month`,{params:{year,month}})}
  weeklySummary(){return this.http.get<WeeklySummary>(`${this.base}/summary-weekly`)}
  monthlySummary(year:number,month:number){return this.http.get<MonthlySummary>(`${this.base}/summary-monthly`,{params:{year,month}})}
  insights(year:number,month:number){return this.http.get<string[]>(`${this.base}/insights`,{params:{year,month}})}
  alerts(){return this.http.get<FinancialAlert[]>(`${this.base}/alerts`)}
  preferences(){return this.http.get<UserPreferenceView>(`${this.base}/preferences`)}
  savePreferences(body:UserPreferenceView){return this.http.put<UserPreferenceView>(`${this.base}/preferences`,body)}
  importPreview(file:File){const form=new FormData();form.append('file',file,file.name);return this.http.post<ImportPreview>(`${this.base}/imports/preview`,form)}
  importConfirm(rows:ImportRowInput[]){return this.http.post<ImportResult>(`${this.base}/imports/confirm`,{rows})}
  conversations(){return this.http.get<ConversationRow[]>('/api/v1/assistant/conversations')}
  conversation(id:string){return this.http.get<ConversationMessage[]>(`/api/v1/assistant/conversations/${id}`)}
  createConversation(title:string){return this.http.post<{id:string}>('/api/v1/assistant/conversations',{title})}
  deleteConversation(id:string){return this.http.delete<void>(`/api/v1/assistant/conversations/${id}`)}
  clearConversations(){return this.http.delete<{removed:number}>('/api/v1/assistant/conversations')}
}

export const PAYMENT_LABELS:Record<PaymentMethod,string>=FINANCE_PAYMENT_LABELS;
export const TRANSACTION_LABELS:Record<TransactionType,string>=FINANCE_TRANSACTION_LABELS;
