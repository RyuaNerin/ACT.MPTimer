namespace ACT.MPTimer
{
    using System.Collections.Generic;

    /// <summary>
    /// ジョブ
    /// </summary>
    public class Job
    {
        private static object lockObject = new object();

        /// <summary>
        /// JobId
        /// </summary>
        public int JobId { get; set; }

        /// <summary>
        /// JobName
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// ジョブリスト
        /// </summary>
        private static List<Job> jobList;

        /// <summary>
        /// ジョブリストを取得する
        /// </summary>
        /// <returns>
        /// ジョブリスト</returns>
        public static Job[] GetJobList()
        {
            lock (lockObject)
            {
                if (jobList == null)
                {
                    var list = new List<Job>();

                    list.Add(new Job() { JobId = 0, JobName = string.Empty });
                    list.Add(new Job() { JobId = 1, JobName = "검술사" });
                    list.Add(new Job() { JobId = 2, JobName = "격투사" });
                    list.Add(new Job() { JobId = 3, JobName = "도끼술사" });
                    list.Add(new Job() { JobId = 4, JobName = "창술사" });
                    list.Add(new Job() { JobId = 5, JobName = "궁술사" });
                    list.Add(new Job() { JobId = 6, JobName = "환술사" });
                    list.Add(new Job() { JobId = 7, JobName = "주술사" });
                    list.Add(new Job() { JobId = 8, JobName = "목수" });
                    list.Add(new Job() { JobId = 9, JobName = "대장장이" });
                    list.Add(new Job() { JobId = 10, JobName = "갑주제작사" });
                    list.Add(new Job() { JobId = 11, JobName = "보석공예가" });
                    list.Add(new Job() { JobId = 12, JobName = "가죽공예까" });
                    list.Add(new Job() { JobId = 13, JobName = "재봉사" });
                    list.Add(new Job() { JobId = 14, JobName = "연금술사" });
                    list.Add(new Job() { JobId = 15, JobName = "요리사" });
                    list.Add(new Job() { JobId = 16, JobName = "광부" });
                    list.Add(new Job() { JobId = 17, JobName = "원예가" });
                    list.Add(new Job() { JobId = 18, JobName = "어부" });
                    list.Add(new Job() { JobId = 19, JobName = "나이트" });
                    list.Add(new Job() { JobId = 20, JobName = "몽크" });
                    list.Add(new Job() { JobId = 21, JobName = "전사" });
                    list.Add(new Job() { JobId = 22, JobName = "용기사" });
                    list.Add(new Job() { JobId = 23, JobName = "음유시인" });
                    list.Add(new Job() { JobId = 24, JobName = "백마도사" });
                    list.Add(new Job() { JobId = 25, JobName = "흑마도사" });
                    list.Add(new Job() { JobId = 26, JobName = "비술사" });
                    list.Add(new Job() { JobId = 27, JobName = "소환사" });
                    list.Add(new Job() { JobId = 28, JobName = "학자" });
                    list.Add(new Job() { JobId = 29, JobName = "쌍검사" });
                    list.Add(new Job() { JobId = 30, JobName = "닌자" });
                    list.Add(new Job() { JobId = 31, JobName = "기공사" });
                    list.Add(new Job() { JobId = 32, JobName = "암흑기사" });
                    list.Add(new Job() { JobId = 33, JobName = "점성술사" });

                    jobList = list;
                }
            }

            return jobList.ToArray();
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>文字列</returns>
        public override string ToString()
        {
            return this.JobName;
        }
    }
}
