// client/src/app/features/artist-search/artist-search.component.ts
import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GameService } from '../../core/services/game.service';
import { Node } from '../../core/models/graph.model';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-artist-search',
  standalone: true,
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

  readonly searchForm = this.fb.group({
    artistName: ['', [Validators.required, Validators.minLength(2)]]
  });

  // Unified search command channel
  private readonly searchTrigger$ = new Subject<{ query: string; isImmediate: boolean }>();

  constructor() {
    // 🧠 Pipeline 1: Core processing engine
    this.searchTrigger$.pipe(
      switchMap(({ query, isImmediate }) => {
        const trimmedQuery = query.trim();

        // Guard: If input is cleared or too short, reset state smoothly
        if (!trimmedQuery || trimmedQuery.length < 2) {
          this.searchResults.set([]);
          this.isSearching.set(false);
          return of([]);
        }

        this.isSearching.set(true);
        this.errorMessage.set(null);

        const apiCall$ = this.gameService.searchArtists(trimmedQuery).pipe(
          catchError(() => {
            this.errorMessage.set('Search service temporarily unavailable.');
            this.isSearching.set(false);
            return of([]);
          })
        );

        // If clicking/submitting form, execute immediately without latency
        if (isImmediate) {
          return apiCall$;
        }

        // Otherwise, if typing naturally, apply debouncing rules
        return of(null).pipe(
          debounceTime(300),
          switchMap(() => apiCall$)
        );
      }),
      takeUntilDestroyed() // Safe here: constructor provides an active Injection Context
    ).subscribe(nodes => {
      this.searchResults.set(nodes);
      this.isSearching.set(false);
    });

    // 🌟 MOVED HERE FROM ngOnInit()
    // ⏱️ Pipeline 2: Intercept value changes reactively as they type
    this.searchForm.controls.artistName.valueChanges.pipe(
      distinctUntilChanged(),
      takeUntilDestroyed() // Now completely safe from runtime context crashes!
    ).subscribe(value => {
      this.searchTrigger$.next({ query: value, isImmediate: false });
    });
  }

  /**
   * Triggered instantly when clicking the Search button or pressing Enter
   */
  onSearch(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }

    const query = this.searchForm.controls.artistName.value;
    this.searchTrigger$.next({ query, isImmediate: true });
  }
}