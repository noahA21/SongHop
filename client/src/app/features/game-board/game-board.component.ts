// client/src/app/features/game-board/game-board.component.ts
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameService, Node } from '../../core/services/game.service';

export interface HistoricalRun {
  startNodeName: string;
  targetNodeName: string;
  moves: number;
  status: 'Completed' | 'Surrendered';
  timestamp: string;
}

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './game-board.component.html',
  styleUrl: './game-board.scss'
})
export class GameBoardComponent implements OnInit {
  private readonly gameService = inject(GameService);

  readonly gameState = signal<'welcome' | 'playing' | 'victory'>('welcome');
  readonly recentRuns = signal<HistoricalRun[]>([]);

  readonly startNode = signal<Node | null>(null);
  readonly targetNode = signal<Node | null>(null);
  readonly currentNode = signal<Node | null>(null);
  readonly neighbors = signal<Node[]>([]);
  readonly pathHistory = signal<Node[]>([]);
  readonly optimalHops = signal<number | null>(null);
  readonly currentDistance = signal<number | null>(null);
  readonly hintCharges = signal<number>(3);
  
  readonly temperature = signal<'HOT' | 'WARM' | 'COLD' | 'NEUTRAL'>('NEUTRAL');

  readonly isLoading = signal<boolean>(false);
  readonly isValidatingWin = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasWon = signal<boolean>(false);
  readonly isPathVerified = signal<boolean>(false);

  // Updated to track the full Node object for the static inspection panel
  readonly activeInspectedNode = signal<Node | null>(null);

  readonly currentMoveCount = computed(() => this.pathHistory().length);
  readonly visitedNodeIds = computed(() => new Set<string>(this.pathHistory().map(n => n.id)));

  ngOnInit(): void {
    this.loadRunsFromStorage();
  }

  startNewGame(): void {
    this.isLoading.set(true);
    this.isValidatingWin.set(false);
    this.errorMessage.set(null);
    this.hasWon.set(false);
    this.isPathVerified.set(false);
    this.pathHistory.set([]);
    this.optimalHops.set(null);
    this.currentDistance.set(null);
    this.temperature.set('NEUTRAL');
    this.gameState.set('playing');

    this.gameService.startGameSession().subscribe({
      next: (session) => {
        this.startNode.set(session.startNode);
        this.targetNode.set(session.targetNode);
        this.currentNode.set(session.startNode);
        this.optimalHops.set(session.optimalHops);
        this.currentDistance.set(session.optimalHops);
        this.loadNeighbors(session.startNode.id);
      },
      error: () => {
        this.errorMessage.set('Could not initialize a winnable session. Verify server database status.');
        this.isLoading.set(false);
        this.gameState.set('welcome');
      }
    });
    this.hintCharges.set(10); // Reset back to n charges on fresh runs
    this.gameState.set('playing');
  }
  // helps handle the deduction and unmasking logic
  revealNodeHint(artist: Node): void {
    if (this.hintCharges() > 0 && !artist.hintRevealed) {
      this.hintCharges.update(c => c - 1);
      artist.hintRevealed = true;
    }
  }
  loadNeighbors(nodeId: string): void {
    this.isLoading.set(true);
    const target = this.targetNode();
    
    // Extract history to send to the backend
    const visitedArray = Array.from(this.visitedNodeIds());
    
    this.gameService.expandNode(nodeId, target?.id, visitedArray).subscribe({
      next: (res) => {
        this.neighbors.set(res.nodes.slice(0, 5));

        const previousDist = this.currentDistance();
        const incomingDist = res.currentDistance;

        if (previousDist !== null && incomingDist !== null) {
          if (incomingDist < previousDist) this.temperature.set('HOT');
          else if (incomingDist > previousDist) this.temperature.set('COLD');
          else this.temperature.set('WARM');
        } else {
          this.temperature.set('NEUTRAL');
        }

        this.currentDistance.set(res.currentDistance);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to retrieve connected artists from graph network.');
        this.isLoading.set(false);
      }
    });
  }

  makeHop(node: Node): void {
    if (this.visitedNodeIds().has(node.id)) return;

    this.clearInspectedArtist();

    const current = this.currentNode();
    if (current) {
      this.pathHistory.update(history => [...history, current]);
    }

    this.currentNode.set(node);

    if (node.id === this.targetNode()?.id) {
      this.handleWinCondition();
      return;
    }

    this.loadNeighbors(node.id);
  }

  backtrack(): void {
    const history = this.pathHistory();
    if (history.length === 0) return;

    this.clearInspectedArtist();

    const previousNode = history[history.length - 1];
    this.pathHistory.update(h => h.slice(0, -1));
    this.currentNode.set(previousNode);
    
    this.temperature.set('NEUTRAL');
    this.loadNeighbors(previousNode.id);
  }

  abandonMission(): void {
    const start = this.startNode();
    const target = this.targetNode();
    
    if (start && target) {
      const runLog: HistoricalRun = {
        startNodeName: start.name,
        targetNodeName: target.name,
        moves: this.currentMoveCount(),
        status: 'Surrendered',
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      };
      this.recentRuns.update(runs => [runLog, ...runs]);
      this.saveRunsToStorage();
    }
    
    this.gameState.set('welcome');
  }

  setInspectedArtist(node: Node): void {
    this.activeInspectedNode.set(node);
  }

  clearInspectedArtist(): void {
    this.activeInspectedNode.set(null);
  }

  private handleWinCondition(): void {
    this.isValidatingWin.set(true);
    this.neighbors.set([]);

    const fullPathIds = [
      this.startNode()!.id,
      ...this.pathHistory().map(n => n.id),
      this.currentNode()!.id
    ];

    const distinctPathIds = Array.from(new Set(fullPathIds));

    this.gameService.validatePath(distinctPathIds).subscribe({
      next: (result) => {
        this.isValidatingWin.set(false);
        if (result.isValid) {
          this.hasWon.set(true);
          this.isPathVerified.set(true);
          this.gameState.set('victory');

          const start = this.startNode();
          const target = this.targetNode();
          if (start && target) {
            const runLog: HistoricalRun = {
              startNodeName: start.name,
              targetNodeName: target.name,
              moves: result.moveCount,
              status: 'Completed',
              timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
            };
            this.recentRuns.update(runs => [runLog, ...runs]);
            this.saveRunsToStorage();
          }
        } else {
          this.errorMessage.set(result.message || 'Path validation failed.');
          this.gameState.set('playing');
        }
      },
      error: () => {
        this.isValidatingWin.set(false);
        this.errorMessage.set('Server path validation engine failure.');
        this.gameState.set('playing');
      }
    });
  }

  private loadRunsFromStorage(): void {
    const data = localStorage.getItem('songhop_traversal_logs');
    if (data) {
      try { this.recentRuns.set(JSON.parse(data)); } catch { this.recentRuns.set([]); }
    }
  }

  private saveRunsToStorage(): void {
    localStorage.setItem('songhop_traversal_logs', JSON.stringify(this.recentRuns()));
  }
}