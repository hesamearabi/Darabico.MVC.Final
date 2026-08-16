using final_p_01.Domain.Entities;

public class Faq
{
    public int Id { get; set; }
    public string Question { get; set; } // متن سوال
    public string Answer { get; set; }   // متن جواب

    // ارتباط با مقاله
    public int PostId { get; set; }
    public Post? Post { get; set; }
}