// src/app/models/graph.model.ts

export interface Node {
  id: string;
  name: string;
  type: string;
  popularityScore: number;
  imageUrl?: string;   
  externalId?: string; 
  
  // MusicBrainz Metadata Fields
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
  nodes: Node[];
  currentDistance: number | null; // Keeps frontend compile error loops safely closed
}

export interface PathResult {
  nodeIds: string[];
  moveCount: number;
  rarityScore: number;
  isValid: boolean;
}