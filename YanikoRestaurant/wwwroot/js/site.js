document.addEventListener('DOMContentLoaded', function ()
{
    const featuredSection = document.querySelector('.featured-dishes');
    const observer = new IntersectionObserver((entries) =>
    {
        entries.forEach(entry =>
        {
            if (entry.isIntersecting)
            {
                featuredSection.classList.add('visible');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    observer.observe(featuredSection);
});