import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Node {
  id: string;
  name: string;
  type: string;
  popularityScore: number;
}

export interface Edge {
  id: string;
  sourceId: string;
  targetId: string;
  type: string;
  isBidirectional: boolean;
  weight: number;
}

// 🆕 Add an explicit match for your backend's return JSON structure
export interface ExpandNodeResponse {
  edges: any[];
  nodes: Node[];
}

// Matches the backend PathResult record type
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
  private readonly http = inject(HttpClient);
  
  // Double-check your actual running .NET Kestrel port!
  private readonly apiUrl = 'http://localhost:5017/v1'; 

 getTestNodes(): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/test/nodes`);
  }

  // ✅ Updated to expect the complex object matching your backend controller
  expandNode(nodeId: string): Observable<ExpandNodeResponse> {
    return this.http.get<ExpandNodeResponse>(`${this.apiUrl}/node/expand/${nodeId}`);
  }

  getSmartPath(startId: string, targetId: string): Observable<PathResult> {
    return this.http.post<PathResult>(`${this.apiUrl}/path/smart`, {
      startNodeId: startId,
      targetNodeId: targetId
    });
  }

  // ✅ Added to hit your backend validation endpoint when the game wraps up
  validatePath(submittedPath: string[]): Observable<PathResult> {
    return this.http.post<PathResult>(`${this.apiUrl}/path/validate`, {
      submittedPath: submittedPath
    });
  }
}