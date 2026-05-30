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

export interface ExpandNodeResponse {
  edges: any[];
  nodes: Node[];
}

export interface GameSession {
  startNode: Node;
  targetNode: Node;
  optimalHops: number;
}

export interface PathValidationResult {
  isValid: boolean;
  moveCount: number;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class GameService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5017/v1'; 

  // Direct backend interface for artist search feature
  searchArtists(query: string): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/node/search`, {
      params: { q: query }
    });
  }

  // Requests a mathematically verified short-hop session configuration
  startGameSession(): Observable<GameSession> {
    return this.http.get<GameSession>(`${this.apiUrl}/game/start`);
  }

  // Radiates out neighbors bidirectionally
  expandNode(nodeId: string): Observable<ExpandNodeResponse> {
    return this.http.get<ExpandNodeResponse>(`${this.apiUrl}/node/expand/${nodeId}`);
  }

  // Submits complete historical trail for absolute database check confirmation
  validatePath(submittedPath: string[]): Observable<PathValidationResult> {
    return this.http.post<PathValidationResult>(`${this.apiUrl}/path/validate`, {
      submittedPath: submittedPath
    });
  }
}