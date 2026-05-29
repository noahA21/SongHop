// client/src/app/features/game-board/game-board.component.ts
import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { GameService, Node } from '../../core/services/game.service';

@Component({
  selector: 'app-game-board',
  templateUrl: './game-board.component.html',
  styleUrl: './game-board.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class GameBoardComponent implements OnInit {
  private readonly gameService = inject(GameService);

  // --- Core Game State (Signals) ---
  readonly startNode = signal<Node | null>(null);
  readonly targetNode = signal<Node | null>(null);
  readonly currentNode = signal<Node | null>(null);
  readonly neighbors = signal<Node[]>([]);
  readonly pathHistory = signal<Node[]>([]); 
  
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasWon = signal<boolean>(false);
  readonly isPathVerified = signal<boolean>(false); 

  // --- Derived State (Computed Signals) ---
  readonly moveCount = computed(() => this.pathHistory().length);
  
  readonly gameStatusMessage = computed(() => {
    if (this.hasWon()) {
      return this.isPathVerified() 
        ? `Victory verified! You reached the target in ${this.moveCount()} hops!`
        : 'Reaching target... Verifying route integrity with server...';
    }
    if (this.isLoading()) return 'Loading adjacent nodes...';
    return `Currently at ${this.currentNode()?.name ?? 'Unknown Artist'}`;
  });

  ngOnInit(): void {
    this.startNewGame();
  }

  // 🔄 FIXED FLOW: Connects directly to the real data initialization pipeline
  startNewGame(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasWon.set(false);
    this.isPathVerified.set(false);
    this.pathHistory.set([]);

    // 🔄 FIXED: Switch from sequential test nodes to a structured session pair
    this.gameService.startGameSession().subscribe({
      next: (session) => {
        this.startNode.set(session.startNode);
        this.targetNode.set(session.targetNode);
        this.currentNode.set(session.startNode);
        
        // Populate valid branching relationships from the start node
        this.loadNeighbors(session.startNode.id);
      },
      error: () => {
        this.errorMessage.set('Failed to initialize a secure game session with the server.');
        this.isLoading.set(false);
      }
    });
  }

  makeHop(selectedNode: Node): void {
    if (this.isLoading() || this.hasWon()) return;

    this.pathHistory.update((history) => [...history, selectedNode]);
    this.currentNode.set(selectedNode);

    if (selectedNode.id === this.targetNode()?.id) {
      this.hasWon.set(true);
      this.validatePathOnServer(); 
      return;
    }

    this.loadNeighbors(selectedNode.id);
  }

  private loadNeighbors(nodeId: string): void {
    this.isLoading.set(true);
    this.gameService.expandNode(nodeId).subscribe({
      next: (data) => {
        this.neighbors.set(data.nodes || []);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Error fetching adjacent paths.');
        this.isLoading.set(false);
      }
    });
  }

  private validatePathOnServer(): void {
    const start = this.startNode();
    if (!start) return;

    const completeSubmittedPath = [start.id, ...this.pathHistory().map(n => n.id)];

    this.gameService.validatePath(completeSubmittedPath).subscribe({
      next: (result) => {
        if (result.isValid) {
          this.isPathVerified.set(true);
        } else {
          this.errorMessage.set('Server rejected path validation: Illegal movement step caught.');
        }
      },
      error: () => {
        this.errorMessage.set('Target hit, but failed to connect to path validation server.');
      }
    });
  }
}