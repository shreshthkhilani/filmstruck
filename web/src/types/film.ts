export interface Film {
  date: string;
  title: string;
  location: string;
  companions: string[];
}

export interface FilmStats {
  totalCount: number;
  byYear: Record<string, number>;
  byLocation: Record<string, number>;
  byCompanion: Record<string, number>;
}
