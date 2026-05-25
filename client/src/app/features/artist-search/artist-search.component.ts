// client/src/app/features/artist-search/artist-search.component.ts
import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GameService, Node } from '../../core/services/game.service';

@Component({
  selector: 'app-artist-search',
  imports: [ReactiveFormsModule],
  templateUrl: './artist-search.component.html',
  styleUrl: './artist-search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ArtistSearchComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly gameService = inject(GameService);

  // --- State Management via Signals ---
  readonly searchResults = signal<Node[]>([]);
  readonly isSearching = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  // --- Strict Reactive Form Setup ---
  readonly searchForm = this.fb.group({
    artistName: ['', [Validators.required, Validators.minLength(2)]]
  });

  onSearch(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    const query = this.searchForm.value.artistName;
    this.isSearching.set(true);
    this.errorMessage.set(null);

    // Reusing the test node endpoint for now until the backend search endpoint is ready
    this.gameService.getTestNodes().subscribe({
      next: (nodes) => {
        // Simple client-side mock filter for testing the interface mechanics
        const filtered = nodes.filter(node => 
          node.name.toLowerCase().includes(query?.toLowerCase() ?? '')
        );
        this.searchResults.set(filtered);
        this.isSearching.set(false);
      },
      error: () => {
        this.errorMessage.set('Search engine failed to respond.');
        this.isSearching.set(false);
      }
    });
  }
}