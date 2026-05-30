// client/src/app/features/game-board/game-board.component.ts
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameService, Node } from '../../core/services/game.service';

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './game-board.component.html',
  styleUrl: './game-board.scss'
})
export class GameBoardComponent implements OnInit {
  private readonly gameService = inject(GameService);

  // --- Core State Machine (Signals) ---
  readonly startNode = signal<Node | null>(null);
  readonly targetNode = signal<Node | null>(null);
  readonly currentNode = signal<Node | null>(null);
  readonly neighbors = signal<Node[]>([]);
  readonly pathHistory = signal<Node[]>([]); // Keeps track of the traversal trail
  readonly optimalHops = signal<number | null>(null);

  readonly isLoading = signal<boolean>(false);
  readonly isValidatingWin = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly hasWon = signal<boolean>(false);
  readonly isPathVerified = signal<boolean>(false);

  // --- Computed Stats ---
  readonly currentMoveCount = computed(() => this.pathHistory().length);
  
  // Creates a quick lookup set of IDs currently in the trail to block infinite back-and-forth loops
  readonly visitedNodeIds = computed(() => {
    return new Set<string>(this.pathHistory().map(n => n.id));
  });

  ngOnInit(): void {
    this.startNewGame();
  }

  startNewGame(): void {
    this.isLoading.set(true);
    this.isValidatingWin.set(false);
    this.errorMessage.set(null);
    this.hasWon.set(false);
    this.isPathVerified.set(false);
    this.pathHistory.set([]);
    this.optimalHops.set(null);

    this.gameService.startGameSession().subscribe({
      next: (session) => {
        this.startNode.set(session.startNode);
        this.targetNode.set(session.targetNode);
        this.currentNode.set(session.startNode);
        this.optimalHops.set(session.optimalHops);
        
        // Seed the initial board view with the first artist's neighbors
        this.loadNeighbors(session.startNode.id);
      },
      error: () => {
        this.errorMessage.set('Could not initialize a winnable game session. Ensure the backend server is running.');
        this.isLoading.set(false);
      }
    });
  }

  loadNeighbors(nodeId: string): void {
    this.isLoading.set(true);
    this.gameService.expandNode(nodeId).subscribe({
      next: (res) => {
        this.neighbors.set(res.nodes);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to retrieve connected artists from graph network.');
        this.isLoading.set(false);
      }
    });
  }

  makeHop(node: Node): void {
    // Edge Case Guard: Block choice if already visited in this trail
    if (this.visitedNodeIds().has(node.id)) return;

    // Push current position to history trail before moving forward
    const current = this.currentNode();
    if (current) {
      this.pathHistory.update(history => [...history, current]);
    }

    this.currentNode.set(node);

    // 🏆 WIN CONDITION INTERCEPTOR
    if (node.id === this.targetNode()?.id) {
      this.handleWinCondition();
      return;
    }

    // Continue normal path expansion
    this.loadNeighbors(node.id);
  }

  /**
   * Edge Case 1 Solution: Safely steps backward if the user is trapped or makes a mistake
   */
  backtrack(): void {
    const history = this.pathHistory();
    if (history.length === 0) return; // Nowhere to go back to

    // Extract the previous node
    const previousNode = history[history.length - 1];
    
    // Drop the last item from the state signal array
    this.pathHistory.update(h => h.slice(0, -1));
    this.currentNode.set(previousNode);

    // Reload the previous neighborhood view
    this.loadNeighbors(previousNode.id);
  }

  private handleWinCondition(): void {
    this.isValidatingWin.set(true);
    this.neighbors.set([]); // Clear active board to freeze interactions

    // Compile entire path sequence: Start -> History Track -> Destination
    const fullPathIds = [
      this.startNode()!.id,
      ...this.pathHistory().map(n => n.id),
      this.currentNode()!.id
    ];

    // Filter out duplicates if the start node got caught in transitions
    const distinctPathIds = Array.from(new Set(fullPathIds));

    this.gameService.validatePath(distinctPathIds).subscribe({
      next: (result) => {
        this.isValidatingWin.set(false);
        if (result.isValid) {
          this.hasWon.set(true);
          this.isPathVerified.set(true);
        } else {
          this.errorMessage.set(result.message || 'Path authentication failed. Local trace graph inconsistent.');
        }
      },
      error: () => {
        this.isValidatingWin.set(false);
        this.errorMessage.set('Server validation timed out or failed to parse.');
      }
    });
  }
}