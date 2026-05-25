import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Node {
  id: string;
  name: string;
  type: string;
  popularityScore: number;
}

export interface PathResult {
  nodeIds: string[];
  moveCount: number;
  rarityScore: number;
  isValid: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class GameService {
  // Using modern inject() function instead of constructor injection
  private readonly http = inject(HttpClient);
  
  // Update this to match your .NET API port!
  private readonly apiUrl = 'http://localhost:5017/v1'; 

  getTestNodes(): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/test/nodes`);
  }

  expandNode(nodeId: string): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/node/expand/${nodeId}`);
  }

  getSmartPath(startId: string, targetId: string): Observable<PathResult> {
    return this.http.post<PathResult>(`${this.apiUrl}/path/smart`, {
      startNodeId: startId,
      targetNodeId: targetId
    });
  }
}