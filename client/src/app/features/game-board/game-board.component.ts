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
  readonly pathHistory = signal<Node[]>([]); // Tracks steps taken AFTER the start node
  
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasWon = signal<boolean>(false);
  readonly isPathVerified = signal<boolean>(false); // 🆕 Added to track server validation status

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

  startNewGame(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.hasWon.set(false);
    this.isPathVerified.set(false);
    this.pathHistory.set([]);

    this.gameService.getTestNodes().subscribe({
      next: (nodes) => {
        if (nodes.length >= 2) {
          this.startNode.set(nodes[0]);
          this.targetNode.set(nodes[1]);
          this.currentNode.set(nodes[0]);
          
          // Load the initial available choices radiating from the start node
          this.loadNeighbors(nodes[0].id);
        } else {
          this.errorMessage.set('Insufficient data returned to initialize game.');
          this.isLoading.set(false);
        }
      },
      error: () => {
        this.errorMessage.set('Failed to connect to the pathfinding engine.');
        this.isLoading.set(false);
      }
    });
  }

  makeHop(selectedNode: Node): void {
    if (this.isLoading() || this.hasWon()) return;

    // Append to path history and advance current position
    this.pathHistory.update((history) => [...history, selectedNode]);
    this.currentNode.set(selectedNode);

    // Check Win Condition
    if (selectedNode.id === this.targetNode()?.id) {
      this.hasWon.set(true);
      this.validatePathOnServer(); // 🆕 Call server anti-cheat verification on victory
      return;
    }

    // Otherwise, expand the new node to fetch next possible hops
    this.loadNeighbors(selectedNode.id);
  }

  private loadNeighbors(nodeId: string): void {
  this.isLoading.set(true);
  this.gameService.expandNode(nodeId).subscribe({
    next: (data) => {
      // 🔄 Dig into the response object to pull out just the nodes array
      this.neighbors.set(data.nodes || []);
      this.isLoading.set(false);
    },
    error: () => {
      this.errorMessage.set('Error fetching adjacent paths.');
      this.isLoading.set(false);
    }
  });
}

  /**
   * 🆕 Assembles full traversal trace history and ensures it matches backend relationships
   */
  private validatePathOnServer(): void {
    const start = this.startNode();
    if (!start) return;

    // Combine starting ID with consecutive path history entries to form complete traversal trace
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