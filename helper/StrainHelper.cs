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

        private static bool isInBreakTime(double time, Beatmap beatmap)
        {
            var breaks = beatmap.breaks;
            foreach (var breakTime in breaks)
            {
                double startTime = breakTime.GetRealStart(beatmap);
                double endTime = breakTime.GetRealEnd(beatmap);

                if (startTime <= time && time <= endTime)
                {
                    return true;
                }
            }

            return false;
        }
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

                    // Check whether is in break time or not, if yes then strain is 0.
                    if (isInBreakTime(currentSectionEnd, beatmap))
                        strainValue.Add(0);
                    else
                        strainValue.Add(skill.currentStrain);

                    currentSectionEnd += sectionLength;
                }
                skill.Process(hitObject);
            }

            // Add extra point so it doesn't end abruptly on chart
            strainTime.Add(currentSectionEnd);
            strainValue.Add(0);

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