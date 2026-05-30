// client/src/app/core/services/game.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Node {
  id: string;
  name: string;
  type: string;
  popularityScore: number;
  connectionReason?: string; // Captures relationship "Liner Notes" text strings
}

export interface ExpandNodeResponse {
  nodes: Node[];
  currentDistance: number | null;
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

  searchArtists(query: string): Observable<Node[]> {
    return this.http.get<Node[]>(`${this.apiUrl}/node/search`, {
      params: { q: query }
    });
  }

  startGameSession(): Observable<GameSession> {
    return this.http.get<GameSession>(`${this.apiUrl}/game/start`);
  }

  expandNode(nodeId: string, targetId?: string): Observable<ExpandNodeResponse> {
    let params = new HttpParams();
    if (targetId) {
      params = params.set('targetId', targetId);
    }
    return this.http.get<ExpandNodeResponse>(`${this.apiUrl}/node/expand/${nodeId}`, { params });
  }

  validatePath(submittedPath: string[]): Observable<PathValidationResult> {
    return this.http.post<PathValidationResult>(`${this.apiUrl}/path/validate`, {
      submittedPath: submittedPath
    });
  }
}