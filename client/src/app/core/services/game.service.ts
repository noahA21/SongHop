// client/src/app/core/services/game.service.ts
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

export interface ExpandNodeResponse {
  edges: Edge[];
  nodes: Node[];
}

export interface PathResult {
  nodeIds: string[];
  moveCount: number;
  rarityScore: number;
  isValid: boolean;
}

// 🎮 Explicit interface for the real session initialization endpoint
export interface GameSession {
  startNode: Node;
  targetNode: Node;
}

@Injectable({
  providedIn: 'root'
})
export class GameService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5017/v1'; 

  // 🔍 FIXED: Added the missing search handler to resolve the 404 error
  searchArtists(query: string): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/node/search`, {
      params: { q: query }
    });
  }

  // 🌟 Added to fetch structurally valid games without affecting artist search
  startGameSession(): Observable<GameSession> {
    return this.http.get<GameSession>(`${this.apiUrl}/game/start`);
  }

  expandNode(nodeId: string): Observable<ExpandNodeResponse> {
    return this.http.get<ExpandNodeResponse>(`${this.apiUrl}/node/expand/${nodeId}`);
  }

  getSmartPath(startId: string, targetId: string): Observable<PathResult> {
    return this.http.post<PathResult>(`${this.apiUrl}/path/smart`, {
      startNodeId: startId,
      targetNodeId: targetId
    });
  }

  validatePath(submittedPath: string[]): Observable<PathResult> {
    return this.http.post<PathResult>(`${this.apiUrl}/path/validate`, {
      submittedPath: submittedPath
    });
  }

  getTestNodes(): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/test/nodes`);
  }
}