namespace DialogueDown.Script.Ast;

/// <summary>
/// The weight on a <see cref="WeightedOption"/> in a <see cref="RandomChoice"/>: how likely
/// the engine is to select that option, relative to its siblings. A closed set — a concrete
/// <see cref="NumberWeight"/> percentage or an <see cref="AutoWeight"/> that claims an equal
/// share of the leftover — so a consumer can handle every case exhaustively. A future
/// dynamic, game-state weight would join as a new variant.
/// </summary>
internal abstract record ChoiceWeight;
