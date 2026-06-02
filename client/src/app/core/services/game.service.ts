// client/src/app/core/services/game.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Node {
  id: string;
  name: string;
  type: string;
  popularityScore: number;
  country?: string;
  artistType?: string;
  startYear?: number;
  endYear?: number;
  connectionReason?: string;
  routeHint?: string; // Declared here so the HTML template can bind to it
  hintRevealed?: boolean; //  Client-side visibility toggle
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

  expandNode(nodeId: string, targetId?: string, visitedIds?: string[]): Observable<ExpandNodeResponse> {
    let params = new HttpParams();
    if (targetId) {
      params = params.set('targetId', targetId);
    }
    
    // 👈 NEW: Map the visited IDs to the URL parameters
    if (visitedIds && visitedIds.length > 0) {
      visitedIds.forEach(id => {
        params = params.append('visited', id);
      });
    }
    
    return this.http.get<ExpandNodeResponse>(`${this.apiUrl}/node/expand/${nodeId}`, { params });
  }

  validatePath(submittedPath: string[]): Observable<PathValidationResult> {
    return this.http.post<PathValidationResult>(`${this.apiUrl}/path/validate`, {
      submittedPath: submittedPath
    });
  }
}