using System;
using System.Linq;
using System.Collections.Generic;
using MapsetParser.objects;
using MapsetParser.starrating.standard;

namespace MapsetVerifierBackend.helper
{
    public class StrainHelper
    {
        private const int sectionLength = 400;

        public static DifficultySkillStrain CalculateStrain(Skill skill, Beatmap beatmap)
        {
            List<double> strainTime = new List<double>();
            List<double> strainValue = new List<double>();

            // https://github.com/Naxesss/MapsetParser/blob/master/starrating/standard/StandardDifficultyCalculator.cs#L28-L46
            double currentSectionEnd = sectionLength;
            foreach (HitObject hitObject in beatmap.hitObjects.Skip(1))
            {
                while (hitObject.time > currentSectionEnd)
                {
                    skill.SaveCurrentPeak();
                    skill.StartNewSectionFrom(currentSectionEnd);

                    // Add strains regardless of when they are to have consistent value over all difficulties
                    strainTime.Add(currentSectionEnd);
                    strainValue.Add(skill.currentStrain);
                    currentSectionEnd += sectionLength;
                }

                skill.Process(hitObject);
            }

            return new DifficultySkillStrain
            {
                skillType = skill,
                beatmap = beatmap,
                strainTime = strainTime,
                strainValue = strainValue
            };
        }
    }

    public struct DifficultySkillStrain
    {
        public Skill skillType { get; set; }
        public Beatmap beatmap { get; set; }
        public List<double> strainTime { get; set; }
        public List<double> strainValue { get; set; }
    }
}