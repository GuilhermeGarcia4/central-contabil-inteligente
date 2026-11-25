import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

export interface CommunityDiscussion {
  id:string; title:string; content:string; category:string; authorDisplayName:string; authorRole:string;
  createdAt:string; editedAt:string|null; commentCount:number; usefulCount:number; isUseful:boolean; isMine:boolean; isRemoved:boolean;
}
export interface CommunityComment {
  id:string; content:string; authorDisplayName:string; authorRole:string; createdAt:string; editedAt:string|null; isMine:boolean; isRemoved:boolean;
}
export interface CommunityPage {items:CommunityDiscussion[];total:number;page:number;pageSize:number}
export interface CommunityDetail {discussion:CommunityDiscussion;comments:CommunityComment[]}
export interface CommunityReport {
  id:string; discussionId:string|null; commentId:string|null; reason:string; details:string; status:string;
  createdAt:string; targetType:string; targetTitle:string; targetContent:string; targetAuthor:string;
}
export interface CommunityAction {id:string;actorDisplayName:string;targetType:string;targetId:string;action:string;reason:string;createdAt:string}

@Injectable({providedIn:'root'})
export class CommunityService {
  private readonly http=inject(HttpClient);
  private readonly base='/api/v1/community';
  list(query:string,category:string,page:number){let params=new HttpParams().set('page',page).set('pageSize',20);if(query.trim())params=params.set('q',query.trim());if(category)params=params.set('category',category);return this.http.get<CommunityPage>(this.base,{params})}
  get(id:string){return this.http.get<CommunityDetail>(`${this.base}/${id}`)}
  create(body:{title:string;content:string;category:string}){return this.http.post<CommunityDiscussion>(this.base,body)}
  update(id:string,body:{title:string;content:string;category:string}){return this.http.put<CommunityDiscussion>(`${this.base}/${id}`,body)}
  remove(id:string){return this.http.delete<void>(`${this.base}/${id}`)}
  comment(id:string,content:string){return this.http.post<CommunityComment>(`${this.base}/${id}/comments`,{content})}
  updateComment(id:string,content:string){return this.http.put<CommunityComment>(`${this.base}/comments/${id}`,{content})}
  removeComment(id:string){return this.http.delete<void>(`${this.base}/comments/${id}`)}
  useful(id:string){return this.http.post<{isUseful:boolean}>(`${this.base}/${id}/useful`,{})}
  reportDiscussion(id:string,reason:string,details:string){return this.http.post(`${this.base}/${id}/reports`,{reason,details})}
  reportComment(id:string,reason:string,details:string){return this.http.post(`${this.base}/comments/${id}/reports`,{reason,details})}
  reports(){return this.http.get<CommunityReport[]>(`${this.base}/moderation/reports`)}
  actions(){return this.http.get<CommunityAction[]>(`${this.base}/moderation/actions`)}
  setModerator(email:string,enabled:boolean){return this.http.put<{displayName:string;isModerator:boolean}>(`${this.base}/moderators`,{email,enabled})}
  review(id:string,decision:'Hide'|'Dismiss',reason:string){return this.http.post(`${this.base}/moderation/reports/${id}/review`,{decision,reason})}
}
