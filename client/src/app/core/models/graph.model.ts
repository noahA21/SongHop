// src/app/models/graph.model.ts

export interface Node {
  id: string;
  name: string;
  type: string;
  popularityScore: number;
  imageUrl?: string;   // Matches backend record structural layout
  externalId?: string; //  Holds the MusicBrainz ID UUID link
  
  // 🌟 NEW METADATA FIELDS FROM MUSICBRAINZ ENRICHMENT PHASE
  country?: string;
  artistType?: string;
  startYear?: number;
  endYear?: number;
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
  edges: Edge[]; // Typed explicitly instead of 'any[]' for stricter compiler safety
  nodes: Node[];
}

export interface PathResult {
  nodeIds: string[];
  moveCount: number;
  rarityScore: number;
  isValid: boolean;
}