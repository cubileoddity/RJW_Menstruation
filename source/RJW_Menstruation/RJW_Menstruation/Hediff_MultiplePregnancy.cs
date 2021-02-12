﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using rjw;
using Verse;
using RimWorld;


namespace RJW_Menstruation
{
    public class Hediff_MultiplePregnancy : Hediff_BasePregnancy
    {


		

		public override void GiveBirth()
        {

			if (babies.NullOrEmpty())
			{
				ModLog.Warning(" no babies (debug?) " + this.GetType().Name);
				if (father == null)
				{
					father = Trytogetfather(ref pawn);
				}
				Initialize(pawn, father);
			}

			List<Pawn> siblings = new List<Pawn>();
			foreach (Pawn baby in babies)
			{
				if (xxx.is_animal(baby))
                {
					BestialBirth(baby, siblings);
                }
                else
                {
					HumanlikeBirth(baby, siblings);
                }

			}

			pawn.health.RemoveHediff(this);
		}

		public string GetBabyInfo()
        {
			string res = "";
			if (!babies.NullOrEmpty())
			{
				var babiesdistinct = babies.Distinct(new RaceComparer());
				int iteration = 0;
				foreach (Pawn baby in babiesdistinct)
				{
					int num = babies.Where(x => x.def.Equals(baby.def)).Count();
					if (iteration > 0) res += ", ";
					res += num + " " + baby.def.label;
					iteration++;
				}
				res += " " + Translations.Dialog_WombInfo02;
				return res;
			}
			return "Null";
        }

		public string GetFatherInfo()
        {
			string res = Translations.Dialog_WombInfo03 + ": ";
			if (!babies.NullOrEmpty())
			{
				var babiesdistinct = babies.Distinct(new FatherComparer(pawn));
				int iteration = 0;
				foreach (Pawn baby in babiesdistinct)
				{
					if (iteration > 0) res += ", ";
					res += Utility.GetFather(baby, pawn)?.LabelShort ?? "Unknown";
					iteration++;
				}
				return res;
			}
			return "Null";
		}




        private void HumanlikeBirth(Pawn baby, List<Pawn> siblings)
        {
			Pawn mother = pawn; Pawn father = Utility.GetFather(baby,pawn);
			//backup melanin, LastName for when baby reset by other mod on spawn/backstorychange
			//var skin_whiteness = baby.story.melanin;
			//var last_name = baby.story.birthLastName;

			PawnUtility.TrySpawnHatchedOrBornPawn(baby, mother);

			var sex_need = mother.needs?.TryGetNeed<Need_Sex>();
			if (mother.Faction != null && !(mother.Faction?.IsPlayer ?? false) && sex_need != null)
			{
				sex_need.CurLevel = 1.0f;
			}
			if (mother.Faction != null)
			{
				if (mother.Faction != baby.Faction)
					baby.SetFaction(mother.Faction);
			}
			if (mother.IsPrisonerOfColony)
			{
				baby.guest.CapturedBy(Faction.OfPlayer);
			}

			foreach (Pawn sibling in siblings)
			{
				baby.relations.AddDirectRelation(PawnRelationDefOf.Sibling, sibling);
			}
			siblings.Add(baby);

			PostBirth(mother, father, baby);
		}


		private void BestialBirth(Pawn baby, List<Pawn> siblings)
        {
			Pawn mother = pawn; Pawn father = Utility.GetFather(baby,pawn);
			//backup melanin, LastName for when baby reset by other mod on spawn/backstorychange
			//var skin_whiteness = baby.story.melanin;
			//var last_name = baby.story.birthLastName;

			PawnUtility.TrySpawnHatchedOrBornPawn(baby, mother);

			Need_Sex sex_need = mother.needs?.TryGetNeed<Need_Sex>();
			if (mother.Faction != null && !(mother.Faction?.IsPlayer ?? false) && sex_need != null)
			{
				sex_need.CurLevel = 1.0f;
			}
			if (mother.Faction != null)
			{
				if (mother.Faction != baby.Faction)
					baby.SetFaction(mother.Faction);
			}


			foreach (Pawn sibling in siblings)
			{
				baby.relations.AddDirectRelation(PawnRelationDefOf.Sibling, sibling);
			}
			siblings.Add(baby);
			train(baby, mother, father);

			PostBirth(mother, father, baby);

			//restore melanin, LastName for when baby reset by other mod on spawn/backstorychange
			//baby.story.melanin = skin_whiteness;
			//baby.story.birthLastName = last_name;



		}

        protected override void GenerateBabies()
		{
			AddNewBaby(pawn, father);
		}



		protected void train(Pawn baby, Pawn mother, Pawn father)
		{
			bool _;
			if (!xxx.is_human(baby) && baby.Faction == Faction.OfPlayer)
			{
				if (xxx.is_human(mother) && baby.Faction == Faction.OfPlayer && baby.training.CanAssignToTrain(TrainableDefOf.Obedience, out _).Accepted)
				{
					baby.training.Train(TrainableDefOf.Obedience, mother);
				}
				if (xxx.is_human(mother) && baby.Faction == Faction.OfPlayer && baby.training.CanAssignToTrain(TrainableDefOf.Tameness, out _).Accepted)
				{
					baby.training.Train(TrainableDefOf.Tameness, mother);
				}
			}
		}


		public bool AddNewBaby(Pawn mother, Pawn father)
		{
			float melanin;
			string lastname;
			if (xxx.is_human(mother))
            {
				if (xxx.is_human(father))
                {
					melanin = (mother.story.melanin + father.story.melanin) / 2;
					lastname = NameTriple.FromString(father.Name.ToStringFull).Last;
				}
                else
                {
					melanin = mother.story.melanin;
					lastname = NameTriple.FromString(mother.Name.ToStringFull).Last;
				}

            }
            else
            {
				if (xxx.is_human(father))
                {
					melanin = father.story.melanin;
					lastname = NameTriple.FromString(father.Name.ToStringFull).Last;
				}
				else melanin = Rand.Range(0, 1.0f);
            }
			
			PawnGenerationRequest request = new PawnGenerationRequest(
				newborn: true,
				allowDowned: true,
				canGeneratePawnRelations: false,
				colonistRelationChanceFactor: 0,
				allowFood: false,
				allowAddictions: false,
				relationWithExtraPawnChanceFactor: 0,
				fixedMelanin: melanin,
				kind: BabyPawnKindDecider(mother, father)
				);
			Pawn baby = PawnGenerator.GeneratePawn(request);
			if (baby != null)
            {
				if (xxx.is_human(baby))
				{
					List<Trait> traitpool = new List<Trait>();
					baby.SetMother(mother);
					if (mother != father) baby.SetFather(father);
					
					if (xxx.has_traits(pawn) && pawn.RaceProps.Humanlike)
					{
						foreach (Trait momtrait in pawn.story.traits.allTraits)
						{
							if (!RJWPregnancySettings.trait_filtering_enabled || !non_genetic_traits.Contains(momtrait.def.defName))
								traitpool.Add(momtrait);
						}
					}
					if (father != null && xxx.has_traits(father) && father.RaceProps.Humanlike)
					{
						foreach (Trait poptrait in father.story.traits.allTraits)
						{
							if (!RJWPregnancySettings.trait_filtering_enabled || !non_genetic_traits.Contains(poptrait.def.defName))
								traitpool.Add(poptrait);
						}
					}
					updateTraits(baby, traitpool);

				}
				else
                {
					baby.relations.AddDirectRelation(VariousDefOf.Relation_birthgiver, mother);
					mother.relations.AddDirectRelation(VariousDefOf.Relation_spawn, baby);
					if (mother != father)
					{
						baby.relations.AddDirectRelation(VariousDefOf.Relation_birthgiver, father);
						father.relations.AddDirectRelation(VariousDefOf.Relation_spawn, baby);
					}
                }

				int division = 1;
				while (Rand.Chance(Configurations.EnzygoticTwinsChance) && division < Configurations.MaxEnzygoticTwins) division++;
				for (int i = 0; i < division; i++) babies.Add(baby);
				return true;
            }


			return false;

        }

		

		/// <summary>
		/// Decide pawnkind from mother and father <para/>
		/// Come from RJW
		/// </summary>
		/// <param name="mother"></param>
		/// <param name="father"></param>
		/// <returns></returns>
		public PawnKindDef BabyPawnKindDecider(Pawn mother, Pawn father)
        {
			PawnKindDef spawn_kind_def = mother.kindDef;
			int flag = 0;
			if (xxx.is_human(mother)) flag += 2; 
			if (xxx.is_human(father)) flag += 1;
            //Mother - Father = Flag
			//Human  - Human  =  3
            //Human  - Animal =  2
            //Animal - Human  =  1
            //Animal - Animal =  0

            switch (flag)
            {
				case 3:
					if (!Rand.Chance(RJWPregnancySettings.humanlike_DNA_from_mother)) spawn_kind_def = father.kindDef;
					break;
				case 2:
					if (RJWPregnancySettings.bestiality_DNA_inheritance == 0f) spawn_kind_def = father.kindDef;
					else if (!Rand.Chance(RJWPregnancySettings.bestial_DNA_from_mother)) spawn_kind_def = father.kindDef;
					break;
				case 1:
					if (RJWPregnancySettings.bestiality_DNA_inheritance == 1f) spawn_kind_def = father.kindDef;
					else if (!Rand.Chance(RJWPregnancySettings.bestial_DNA_from_mother)) spawn_kind_def = father.kindDef;
					break;
				case 0:
					if (!Rand.Chance(RJWPregnancySettings.bestial_DNA_from_mother)) spawn_kind_def = father.kindDef;
					break;
            }

			bool IsAndroidmother = AndroidsCompatibility.IsAndroid(mother);
			bool IsAndroidfather = AndroidsCompatibility.IsAndroid(father);
			if (IsAndroidmother && !IsAndroidfather)
			{
				spawn_kind_def = father.kindDef;
			}
			else if (!IsAndroidmother && IsAndroidfather)
			{
				spawn_kind_def = mother.kindDef;
			}


			string MotherRaceName = "";
			string FatherRaceName = "";
			MotherRaceName = mother.kindDef.race.defName;
			if (father != null)
				FatherRaceName = father.kindDef.race.defName;
			if (MotherRaceName != FatherRaceName && FatherRaceName != "")
			{
				var groups = DefDatabase<RaceGroupDef>.AllDefs.Where(x => !(x.hybridRaceParents.NullOrEmpty() || x.hybridChildKindDef.NullOrEmpty()));
				

				//ModLog.Message(" found custom RaceGroupDefs " + groups.Count());
				foreach (var t in groups)
				{
					if ((t.hybridRaceParents.Contains(MotherRaceName) && t.hybridRaceParents.Contains(FatherRaceName))
						|| (t.hybridRaceParents.Contains("Any") && (t.hybridRaceParents.Contains(MotherRaceName) || t.hybridRaceParents.Contains(FatherRaceName))))
					{
						//ModLog.Message(" has hybridRaceParents");
						if (t.hybridChildKindDef.Contains("MotherKindDef"))
							spawn_kind_def = mother.kindDef;
						else if (t.hybridChildKindDef.Contains("FatherKindDef") && father != null)
							spawn_kind_def = father.kindDef;
						else
						{
							//ModLog.Message(" trying hybridChildKindDef " + t.defName);
							var child_kind_def_list = new List<PawnKindDef>();
							child_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => t.hybridChildKindDef.Contains(x.defName)));

							//ModLog.Message(" found custom hybridChildKindDefs " + t.hybridChildKindDef.Count);
							if (!child_kind_def_list.NullOrEmpty())
								spawn_kind_def = child_kind_def_list.RandomElement();
						}
					}
				}



			}
            else
            {
				spawn_kind_def = mother.RaceProps.AnyPawnKind;
            }


			if (spawn_kind_def.defName.Contains("Nymph"))
			{
				//child is nymph, try to find other PawnKindDef
				var spawn_kind_def_list = new List<PawnKindDef>();
				spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == spawn_kind_def.race && !x.defName.Contains("Nymph")));
				//no other PawnKindDef found try mother
				if (spawn_kind_def_list.NullOrEmpty())
					spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == mother.kindDef.race && !x.defName.Contains("Nymph")));
				//no other PawnKindDef found try father
				if (spawn_kind_def_list.NullOrEmpty() && father != null)
					spawn_kind_def_list.AddRange(DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race == father.kindDef.race && !x.defName.Contains("Nymph")));
				//no other PawnKindDef found fallback to generic colonist
				if (spawn_kind_def_list.NullOrEmpty())
					spawn_kind_def = PawnKindDefOf.Colonist;

				spawn_kind_def = spawn_kind_def_list.RandomElement();
			}




			return spawn_kind_def;

        }


		/// <summary>
		/// Copy from RJW
		/// </summary>
		/// <param name="pawn"></param>
		/// <param name="parenttraits"></param>
		public void updateTraits(Pawn pawn, List<Trait> parenttraits)
        {
			if (pawn?.story?.traits == null) return;

			List<Trait> traitlist = new List<Trait>(pawn.story.traits.allTraits);
			if (!parenttraits.NullOrEmpty()) traitlist.AddRange(parenttraits);
			else return;


			var forcedTraits = traitlist
				.Where(x => x.ScenForced)
				.Distinct(new TraitComparer(ignoreDegree: true));

			List<Trait> res = new List<Trait>();
			res.AddRange(forcedTraits);

			var comparer = new TraitComparer(); // trait comparision implementation, because without game compares traits *by reference*, makeing them all unique.

			while (res.Count < traitlist.Count && traitlist.Count > 0)
			{
				int index = Rand.Range(0, traitlist.Count); // getting trait and removing from the pull
				var trait = traitlist[index];
				traitlist.RemoveAt(index);

				if (!res.Any(x => comparer.Equals(x, trait) ||  // skipping traits conflicting with already added
											 x.def.ConflictsWith(trait)))
				{
					res.Add(new Trait(trait.def, trait.Degree, false));
				}
			}


			pawn.story.traits.allTraits = res;
		}



	}

	/// <summary>
	/// Copy from RJW
	/// </summary>
	public class TraitComparer : IEqualityComparer<Trait>
	{
		bool ignoreForced;
		bool ignoreDegree;

		public TraitComparer(bool ignoreDegree = false, bool ignoreForced = true)
		{
			this.ignoreDegree = ignoreDegree;
			this.ignoreForced = ignoreForced;
		}

		public bool Equals(Trait x, Trait y)
		{
			return
				x.def == y.def &&
				(ignoreDegree || (x.Degree == y.Degree)) &&
				(ignoreForced || (x.ScenForced == y.ScenForced));
		}

		public int GetHashCode(Trait obj)
		{
			return
				(obj.def.GetHashCode() << 5) +
				(ignoreDegree ? 0 : obj.Degree) +
				((ignoreForced || obj.ScenForced) ? 0 : 0x10);
		}
	}

    public class RaceComparer : IEqualityComparer<Pawn>
    {
        public bool Equals(Pawn x, Pawn y)
        {
			return x.def.Equals(y.def);
        }

        public int GetHashCode(Pawn obj)
        {
			return obj.def.GetHashCode();
        }
    }

	public class FatherComparer : IEqualityComparer<Pawn>
	{
		Pawn mother;
		
		public FatherComparer(Pawn mother)
        {
			this.mother = mother;
        }

		public bool Equals(Pawn x, Pawn y)
		{
			if (Utility.GetFather(x, mother) == null && Utility.GetFather(y, mother) == null) return true;
			return Utility.GetFather(x,mother)?.Label.Equals(Utility.GetFather(y,mother)?.Label) ?? false;
		}

		public int GetHashCode(Pawn obj)
		{
			return obj.def.GetHashCode();
		}
	}

}