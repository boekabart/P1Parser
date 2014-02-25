using System;
using System.Collections.Generic;

namespace P1Parsing
{
    public class P1Improved
    {
        public static IEnumerable<P1Improved> FromP1Records(IEnumerable<P1Record> srces)
        {
            P1Improved prev = null;
            foreach (var src in srces)
            {
                prev = new P1Improved(prev, src);
                yield return prev;
            }
        }

        public P1Improved(P1Improved prev, P1Record src)
        {
            m_Prev = prev;
            m_Source = src;
            if (m_Prev != null)
                m_Prev.m_Next = this;
        }

        private P1Improved m_Prev;
        private P1Record m_Source;
        private P1Improved m_Next;

        public DateTime DateTime { get { return m_Source.DateTime; }}

        private double? m_kWhTotal;
        private double? m_kW;

        public double kWhTotal
        {
            get
            {
                if (m_kWhTotal.HasValue)
                    return m_kWhTotal.Value;

                var refTotal = m_Source.kWhTotal;
                bool foundFirst, foundLast = false;
                var totalSince = AggregateKwsSinceRefTotal(out foundFirst);
                var totalAfter = m_Next == null? 0:m_Next.AggregateKwsAfterRefTotal(out foundLast);

                if (foundLast && !foundFirst)
                    return refTotal + 1.0 - totalAfter;

                if (!foundLast)
                    return refTotal + totalSince;

                var total = totalSince + totalAfter;
                var factor = total > 0.0 ? 1.0/total : 1.0;
                m_kWhTotal = refTotal + factor*totalSince;
                m_kW = m_Source.kW*factor;
                return m_kWhTotal.Value;
            }
        }

        private double AggregateKwsSinceRefTotal(out bool foundFirst)
        {
            if (m_Prev == null)
            {
                foundFirst = false;
                return 0;
            }
            if (m_Prev.m_Source.kWhTotal != m_Source.kWhTotal)
            {
                foundFirst = true;
                return 0;
            }
            var myConsumption = m_Source.kW*(DateTime - m_Prev.DateTime).TotalHours;
            return myConsumption + m_Prev.AggregateKwsSinceRefTotal(out foundFirst);
        }

        private double AggregateKwsAfterRefTotal(out bool foundLast)
        {
            var myConsumption = m_Source.kW * (DateTime - m_Prev.DateTime).TotalHours;
            if ((int)m_Prev.m_Source.kWhTotal != (int)m_Source.kWhTotal)
            {
                foundLast = true;
                return myConsumption;
            }

            if (m_Next != null)
                return myConsumption + m_Next.AggregateKwsAfterRefTotal(out foundLast);

            foundLast = false;
            return myConsumption;
        }

        public double kW {
            get
            {
                return m_kW.HasValue ? m_kW.Value : m_Source.kW;
            }
        }
        public double M3Total { get { return m_Source.M3Total; } }
        //public P1Record AsP1Record { get { return new P1Record(this); } }
    }
}